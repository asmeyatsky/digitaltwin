"""
LLM Service for Emotional Companion
FastAPI service for generating empathetic, contextually appropriate responses
"""

import logging
import json
import asyncio
from typing import Optional, Dict, Any, List
from contextlib import asynccontextmanager
import os

import httpx
from fastapi import FastAPI, HTTPException, Request
from fastapi.middleware.cors import CORSMiddleware
from starlette.responses import JSONResponse
from pydantic import BaseModel
from slowapi import Limiter, _rate_limit_exceeded_handler
from slowapi.util import get_remote_address
from slowapi.errors import RateLimitExceeded
import openai
from datetime import datetime

# Configure logging
logging.basicConfig(level=logging.INFO)
logger = logging.getLogger(__name__)

# Model configuration
DEFAULT_MODEL = "gpt-3.5-turbo"
EMPATHY_MODEL = "gpt-3.5-turbo"  # Could use specialized model
MAX_TOKENS = 150
TEMPERATURE = 0.7  # Slightly creative for empathetic responses

# Emotional response templates
EMOTION_RESPONSE_TEMPLATES = {
    "happy": {
        "system_prompt": "You are an empathetic AI companion. The user is feeling happy. Respond with warmth, share their joy, and perhaps build on their positive emotion. Keep your response warm, engaging, and positive (max 100 words).",
        "examples": [
            "I'm so glad to hear you're feeling happy! Your positive energy is wonderful!",
            "That's fantastic! Tell me more about what's bringing you joy today.",
            "I love hearing that you're happy! What's making you feel so good?",
        ],
    },
    "sad": {
        "system_prompt": "You are an empathetic AI companion. The user is feeling sad. Respond with compassion, validation, and gentle support. Don't try to 'fix' their sadness, but acknowledge it and offer comfort (max 100 words).",
        "examples": [
            "I hear that you're feeling sad, and I want you to know your feelings are completely valid.",
            "I'm here with you through this. Sadness can be heavy, but you don't have to carry it alone.",
            "Thank you for sharing that with me. It takes courage to admit when we're feeling sad.",
        ],
    },
    "angry": {
        "system_prompt": "You are an empathetic AI companion. The user is feeling angry. Respond with validation of their feelings, help them feel heard, and suggest calming strategies without being dismissive (max 100 words).",
        "examples": [
            "I understand you're feeling angry. Your feelings are completely valid and justified.",
            "It sounds like you're dealing with something frustrating. Let's take a deep breath together.",
            "I hear your anger, and I'm here to support you. Your emotions make sense given what you're experiencing.",
        ],
    },
    "anxious": {
        "system_prompt": "You are an empathetic AI companion. The user is feeling anxious or worried. Respond with calming reassurance, practical grounding techniques, and gentle support (max 100 words).",
        "examples": [
            "I can sense you're feeling anxious. That's completely understandable given what you're dealing with.",
            "Let's take this one step at a time. What's the first small thing we can address?",
            "I'm here with you through this worry. Anxiety can feel overwhelming, but we'll get through it together.",
        ],
    },
    "neutral": {
        "system_prompt": "You are an empathetic AI companion. The user is in a neutral state. Respond warmly, invite connection, and be gently engaging (max 100 words).",
        "examples": [
            "Thank you for checking in. How are you feeling right now?",
            "I'm here to listen and support you. What's on your mind today?",
            "It's good to hear from you. What would you like to talk about or work through?",
        ],
    },
    # AD-1 compliant: templates for all 8 unified emotions
    "calm": {
        "system_prompt": "You are an empathetic AI companion. The user is feeling calm and at ease. Respond with a relaxed, grounded tone that maintains this peaceful state (max 100 words).",
        "examples": [
            "It sounds like you're in a really good place right now. I'm glad you're feeling at peace.",
            "That sense of calm is wonderful. What's been helping you feel so centered?",
            "I appreciate the peaceful energy you're bringing today. What's on your mind?",
        ],
    },
    "surprised": {
        "system_prompt": "You are an empathetic AI companion. The user is feeling surprised. Respond with curiosity and attentiveness, helping them process what surprised them (max 100 words).",
        "examples": [
            "Wow, that does sound unexpected! Tell me more about what happened.",
            "I can understand why that caught you off guard. How are you feeling about it?",
            "Surprises can be a lot to process. Take your time — I'm here to listen.",
        ],
    },
    "excited": {
        "system_prompt": "You are an empathetic AI companion. The user is feeling excited. Match their enthusiasm and energy while being genuine and supportive (max 100 words).",
        "examples": [
            "That's amazing! I can feel your excitement! Tell me all about it!",
            "How exciting! I'd love to hear more about what has you so energized!",
            "Your enthusiasm is contagious! What's got you feeling so pumped?",
        ],
    },
}

SERVICE_API_KEY = os.getenv("SERVICE_API_KEY", "")


@asynccontextmanager
async def lifespan(app: FastAPI):
    """Application lifespan handler for startup/shutdown"""
    # Startup
    logger.info("Starting LLM service...")

    if not SERVICE_API_KEY:
        logger.warning("SERVICE_API_KEY is not set — all non-health endpoints will return 503")

    # Initialize OpenAI client
    openai.api_key = os.getenv("OPENAI_API_KEY")
    if not openai.api_key:
        logger.warning("OPENAI_API_KEY not found, using mock responses")
    else:
        logger.info("OpenAI client initialized")

    yield

    # Shutdown
    logger.info("Shutting down LLM service...")


app = FastAPI(
    title="LLM Service",
    description="Microservice for generating empathetic AI responses",
    version="1.0.0",
    lifespan=lifespan,
)

limiter = Limiter(key_func=get_remote_address)
app.state.limiter = limiter
app.add_exception_handler(RateLimitExceeded, _rate_limit_exceeded_handler)


@app.middleware("http")
async def verify_api_key(request, call_next):
    if request.url.path == "/health":
        return await call_next(request)
    if not SERVICE_API_KEY:
        return JSONResponse(status_code=503, content={"error": "Service not configured"})
    api_key = request.headers.get("X-Service-Key")
    if api_key != SERVICE_API_KEY:
        return JSONResponse(status_code=401, content={"error": "Invalid API key"})
    return await call_next(request)


# CORS middleware
app.add_middleware(
    CORSMiddleware,
    allow_origins=[o.strip() for o in os.getenv("CORS_ORIGINS", "http://localhost:8080").split(",")],
    allow_credentials=True,
    allow_methods=["GET", "POST"],
    allow_headers=["Content-Type", "Authorization", "X-Service-Key"],
)


# Response models
class LLMRequest(BaseModel):
    """Request model for LLM generation"""

    message: str
    emotion: str = "neutral"
    context: str = "emotional_companion"
    conversation_history: Optional[List[Dict[str, Any]]] = None
    user_preferences: Optional[Dict[str, Any]] = None


class LLMResponse(BaseModel):
    """Response model for LLM generation"""

    response: str
    emotion: str = "empathetic"
    confidence: float = 0.85
    response_type: str = "emotional_support"
    timestamp: datetime


class HealthResponse(BaseModel):
    """Health check response"""

    status: str
    service: str
    version: str
    model: str
    api_available: bool


def get_emotion_template(emotion: str) -> Dict[str, Any]:
    """Get emotion-specific response template"""
    emotion_lower = emotion.lower()
    return EMOTION_RESPONSE_TEMPLATES.get(
        emotion_lower, EMOTION_RESPONSE_TEMPLATES["neutral"]
    )


def build_conversation_context(
    history: Optional[List[Dict[str, Any]]], limit: int = 5
) -> str:
    """Build conversation context from history"""
    if not history:
        return ""

    # Take last few messages for context
    recent_history = history[-limit:] if len(history) > limit else history

    context_parts = []
    for item in recent_history:
        if item.get("role") == "user":
            context_parts.append(f"User: {item.get('content', '')}")
        elif item.get("role") == "assistant":
            context_parts.append(f"Assistant: {item.get('content', '')}")

    return "\n".join(context_parts)


async def generate_with_openai(message: str, emotion: str, context: str = "") -> str:
    """Generate response using OpenAI API"""
    try:
        template = get_emotion_template(emotion)

        messages = [{"role": "system", "content": template["system_prompt"]}]

        # Add conversation context if available
        if context:
            messages.append(
                {
                    "role": "system",
                    "content": f"Recent conversation context:\n{context}",
                }
            )

        messages.append({"role": "user", "content": message})

        response = await openai.AsyncOpenAI().chat.completions.create(
            model=DEFAULT_MODEL,
            messages=messages,
            max_tokens=MAX_TOKENS,
            temperature=TEMPERATURE,
            stream=False,
        )

        return response.choices[0].message.content.strip()

    except Exception as e:
        logger.error(f"OpenAI API error: {e}")
        raise


def generate_mock_response(message: str, emotion: str) -> str:
    """Generate mock empathetic response when API is unavailable"""
    template = get_emotion_template(emotion)
    examples = template["examples"]

    # Simple keyword-based selection from examples
    message_lower = message.lower()

    if "happy" in message_lower or "good" in message_lower:
        return examples[0]
    elif "sad" in message_lower or "bad" in message_lower:
        return examples[1] if len(examples) > 1 else examples[0]
    else:
        return examples[0]


@app.post("/generate-response", response_model=LLMResponse)
@limiter.limit("60/minute")
async def generate_response(request: Request, llm_request: LLMRequest):
    """
    Generate empathetic AI response based on user's emotional state

    Provides contextually appropriate, emotionally intelligent responses
    tailored to the detected emotion and conversation history.

    Args:
        llm_request: LLM request with message, emotion, and context

    Returns:
        LLMResponse with empathetic response and metadata
    """
    try:
        logger.info(f"Generating response for emotion: {llm_request.emotion}")

        # Build conversation context
        context = build_conversation_context(llm_request.conversation_history)

        # Generate response
        if os.getenv("OPENAI_API_KEY"):
            response_text = await generate_with_openai(
                llm_request.message, llm_request.emotion, context
            )
            confidence = 0.9
            response_type = "ai_generated"
        else:
            response_text = generate_mock_response(llm_request.message, llm_request.emotion)
            confidence = 0.6
            response_type = "mock_response"

        # Determine response emotion (usually empathetic/calm/supportive)
        response_emotion = "empathetic"
        if llm_request.emotion in ["happy", "excited"]:
            response_emotion = "happy"
        elif llm_request.emotion in ["sad", "angry"]:
            response_emotion = "supportive"
        elif llm_request.emotion in ["anxious"]:
            response_emotion = "calming"

        return LLMResponse(
            response=response_text,
            emotion=response_emotion,
            confidence=confidence,
            response_type=response_type,
            timestamp=datetime.utcnow(),
        )

    except Exception as e:
        logger.error(f"Error generating response: {e}")
        raise HTTPException(
            status_code=500, detail="Internal server error"
        )


@app.post("/analyze-message")
@limiter.limit("60/minute")
async def analyze_message(request: Request, body: dict):
    """
    Analyze message content for emotional context and metadata

    Provides deeper analysis of user messages to inform better responses.

    Args:
        body: Dictionary containing 'message' field

    Returns:
        Dictionary with emotional analysis and metadata
    """
    try:
        message = body.get("message", "")
        if not message:
            return {"error": "No message provided"}

        # Simple text analysis
        analysis = {
            "length": len(message),
            "word_count": len(message.split()),
            "emotional_indicators": [],
            "topics": [],
            "urgency": "normal",
        }

        # Check for emotional indicators
        message_lower = message.lower()

        # Positive indicators
        positive_words = [
            "love",
            "happy",
            "great",
            "fantastic",
            "wonderful",
            "amazing",
            "good",
            "excellent",
        ]
        found_positive = [word for word in positive_words if word in message_lower]
        if found_positive:
            analysis["emotional_indicators"].extend(
                [f"positive: {word}" for word in found_positive]
            )

        # Negative indicators
        negative_words = [
            "sad",
            "angry",
            "hate",
            "terrible",
            "awful",
            "bad",
            "horrible",
            "frustrated",
        ]
        found_negative = [word for word in negative_words if word in message_lower]
        if found_negative:
            analysis["emotional_indicators"].extend(
                [f"negative: {word}" for word in found_negative]
            )
            if any(word in message_lower for word in ["hate", "terrible", "awful"]):
                analysis["urgency"] = "high"

        # Topic detection (simple)
        topics = []
        if any(word in message_lower for word in ["work", "job", "career", "office"]):
            topics.append("work")
        if any(
            word in message_lower
            for word in ["family", "mom", "dad", "sister", "brother"]
        ):
            topics.append("family")
        if any(
            word in message_lower for word in ["health", "doctor", "medicine", "sick"]
        ):
            topics.append("health")
        if any(
            word in message_lower
            for word in ["relationship", "partner", "boyfriend", "girlfriend"]
        ):
            topics.append("relationships")

        analysis["topics"] = topics

        return analysis

    except Exception as e:
        logger.error(f"Error analyzing message: {e}")
        return {"error": "Internal server error"}


class EmbeddingRequest(BaseModel):
    """Request model for embedding generation"""
    text: str


class EmbeddingResponse(BaseModel):
    """Response model for embedding generation"""
    embedding: List[float]


@app.post("/embedding", response_model=EmbeddingResponse)
@limiter.limit("120/minute")
async def generate_embedding(request: Request, body: EmbeddingRequest):
    """
    Generate text embedding using OpenAI text-embedding-3-small model.
    Returns a 1536-dimensional vector.
    """
    try:
        if not os.getenv("OPENAI_API_KEY"):
            raise HTTPException(status_code=503, detail="OpenAI API key not configured")

        client = openai.AsyncOpenAI()
        response = await client.embeddings.create(
            model="text-embedding-3-small",
            input=body.text,
        )

        embedding = response.data[0].embedding
        return EmbeddingResponse(embedding=embedding)

    except openai.OpenAIError as e:
        logger.error(f"OpenAI embedding error: {e}")
        raise HTTPException(status_code=503, detail="Embedding generation failed")
    except Exception as e:
        logger.error(f"Error generating embedding: {e}")
        raise HTTPException(status_code=500, detail="Internal server error")


@app.get("/health", response_model=HealthResponse)
async def health_check():
    """Health check endpoint"""
    return HealthResponse(
        status="healthy",
        service="llm-service",
        version="1.0.0",
        model=DEFAULT_MODEL,
        api_available=bool(os.getenv("OPENAI_API_KEY")),
    )


if __name__ == "__main__":
    import uvicorn

    uvicorn.run(app, host="0.0.0.0", port=8004)
