"""
LLM Service for Emotional Companion
FastAPI service for generating empathetic, contextually appropriate responses
Supports multiple LLM providers: OpenAI, Anthropic (Claude), Google (Gemini)
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
from datetime import datetime

from providers import create_llm_provider, LLMProvider
from embeddings import create_embedding_provider, EmbeddingProvider

# Configure logging
logging.basicConfig(level=logging.INFO)
logger = logging.getLogger(__name__)

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

# Module-level provider references (set during lifespan)
llm_provider: LLMProvider | None = None
embedding_provider: EmbeddingProvider | None = None


@asynccontextmanager
async def lifespan(app: FastAPI):
    """Application lifespan handler for startup/shutdown"""
    global llm_provider, embedding_provider

    # Startup
    logger.info("Starting LLM service...")

    if not SERVICE_API_KEY:
        logger.warning("SERVICE_API_KEY is not set — all non-health endpoints will return 503")

    # Initialize providers via factories
    llm_provider = create_llm_provider()
    embedding_provider = create_embedding_provider()

    if not llm_provider.is_available():
        logger.warning(
            f"LLM provider '{llm_provider.provider_name}' has no API key configured, "
            "using mock responses"
        )
    else:
        logger.info(
            f"LLM provider '{llm_provider.provider_name}' initialized "
            f"(model: {llm_provider.model})"
        )

    if not embedding_provider.is_available():
        logger.warning(
            f"Embedding provider '{embedding_provider.provider_name}' is not available"
        )

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
    llm_provider: str = "openai"
    embedding_provider: str = "openai"
    embedding_dimension: int = 1536


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
    """
    try:
        logger.info(f"Generating response for emotion: {llm_request.emotion}")

        # Build conversation context
        context = build_conversation_context(llm_request.conversation_history)
        template = get_emotion_template(llm_request.emotion)

        # Generate response
        if llm_provider and llm_provider.is_available():
            try:
                response_text = await llm_provider.generate_chat_response(
                    system_prompt=template["system_prompt"],
                    user_message=llm_request.message,
                    context=context,
                )
                confidence = 0.9
                response_type = "ai_generated"
            except Exception as e:
                logger.warning(f"LLM provider error, falling back to mock: {e}")
                response_text = generate_mock_response(llm_request.message, llm_request.emotion)
                confidence = 0.6
                response_type = "mock_response"
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


class EmotionAnalysisRequest(BaseModel):
    """Request for text emotion analysis"""
    text: str


class EmotionAnalysisResponse(BaseModel):
    """Response for text emotion analysis"""
    emotion: str
    confidence: float
    all_emotions: Dict[str, float] = {}


class EmbeddingRequest(BaseModel):
    """Request model for embedding generation"""
    text: str


class EmbeddingResponse(BaseModel):
    """Response model for embedding generation"""
    embedding: List[float]


@app.post("/embedding", response_model=EmbeddingResponse)
@limiter.limit("120/minute")
async def generate_embedding_endpoint(request: Request, body: EmbeddingRequest):
    """
    Generate text embedding using the configured embedding provider.
    """
    try:
        if not embedding_provider or not embedding_provider.is_available():
            raise HTTPException(
                status_code=503,
                detail=f"Embedding provider '{embedding_provider.provider_name if embedding_provider else 'none'}' not configured",
            )

        embedding = await embedding_provider.generate_embedding(body.text)
        return EmbeddingResponse(embedding=embedding)

    except HTTPException:
        raise
    except Exception as e:
        logger.error(f"Embedding error ({embedding_provider.provider_name if embedding_provider else 'none'}): {e}")
        raise HTTPException(status_code=503, detail="Embedding generation failed")


@app.get("/health", response_model=HealthResponse)
async def health_check():
    """Health check endpoint"""
    return HealthResponse(
        status="healthy",
        service="llm-service",
        version="1.0.0",
        model=llm_provider.model if llm_provider else "none",
        api_available=bool(llm_provider and llm_provider.is_available()),
        llm_provider=llm_provider.provider_name if llm_provider else "none",
        embedding_provider=embedding_provider.provider_name if embedding_provider else "none",
        embedding_dimension=embedding_provider.dimension if embedding_provider else 0,
    )


# AD-1 compliant emotion list
AD1_EMOTIONS = ["happy", "sad", "angry", "anxious", "calm", "surprised", "excited", "neutral"]


def _keyword_emotion_fallback(text: str) -> tuple[str, float]:
    """Keyword-based emotion fallback when API is unavailable."""
    text_lower = text.lower()

    keyword_map = {
        "happy": ["happy", "joy", "wonderful", "great", "amazing", "love", "fantastic", "glad"],
        "sad": ["sad", "depressed", "down", "unhappy", "terrible", "disappointed", "lonely", "cry"],
        "angry": ["angry", "mad", "furious", "annoyed", "frustrated", "irritated", "upset", "rage"],
        "anxious": ["anxious", "worried", "nervous", "scared", "afraid", "fearful", "panic", "terrified"],
        "calm": ["calm", "peaceful", "relaxed", "serene", "tranquil", "content", "zen"],
        "surprised": ["surprised", "shocked", "amazed", "astonished", "wow", "incredible", "unbelievable"],
        "excited": ["excited", "thrilled", "pumped", "eager", "enthusiastic", "hyped", "stoked"],
    }

    for emotion, keywords in keyword_map.items():
        matches = sum(1 for kw in keywords if kw in text_lower)
        if matches > 0:
            confidence = min(0.4 + matches * 0.15, 0.85)
            return emotion, confidence

    return "neutral", 0.5


@app.post("/analyze-emotion-text", response_model=EmotionAnalysisResponse)
@limiter.limit("60/minute")
async def analyze_emotion_text(request: Request, body: EmotionAnalysisRequest):
    """
    Analyze text to detect the primary emotion using the configured LLM provider.
    Falls back to keyword-based detection when API is unavailable.
    Returns AD-1 compliant emotion classification.
    """
    try:
        if llm_provider and llm_provider.is_available():
            try:
                result = await llm_provider.classify_emotion(body.text, AD1_EMOTIONS)

                emotion = result.get("emotion", "neutral").lower()
                if emotion not in AD1_EMOTIONS:
                    emotion = "neutral"

                confidence = min(max(float(result.get("confidence", 0.7)), 0.0), 1.0)

                return EmotionAnalysisResponse(
                    emotion=emotion,
                    confidence=round(confidence, 2),
                )

            except Exception as e:
                logger.warning(f"LLM emotion analysis failed, using fallback: {e}")
                emotion, confidence = _keyword_emotion_fallback(body.text)
                return EmotionAnalysisResponse(
                    emotion=emotion,
                    confidence=round(confidence, 2),
                )
        else:
            emotion, confidence = _keyword_emotion_fallback(body.text)
            return EmotionAnalysisResponse(
                emotion=emotion,
                confidence=round(confidence, 2),
            )

    except Exception as e:
        logger.error(f"Emotion analysis error: {e}")
        raise HTTPException(status_code=500, detail="Emotion analysis failed")


if __name__ == "__main__":
    import uvicorn

    uvicorn.run(app, host="0.0.0.0", port=8004)
