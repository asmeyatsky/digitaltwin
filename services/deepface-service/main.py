"""
DeepFace Emotion Recognition Microservice
FastAPI service for facial expression and emotion detection
"""

import io
import logging
from typing import Optional
from contextlib import asynccontextmanager

import numpy as np
from fastapi import FastAPI, HTTPException, UploadFile, File
from fastapi.middleware.cors import CORSMiddleware
from pydantic import BaseModel
from PIL import Image

# Configure logging
logging.basicConfig(level=logging.INFO)
logger = logging.getLogger(__name__)

# DeepFace import (lazy loaded for faster startup)
deepface = None


def get_deepface():
    """Lazy load DeepFace to improve startup time"""
    global deepface
    if deepface is None:
        from deepface import DeepFace
        deepface = DeepFace
        logger.info("DeepFace loaded successfully")
    return deepface


@asynccontextmanager
async def lifespan(app: FastAPI):
    """Application lifespan handler for startup/shutdown"""
    # Startup: Pre-load DeepFace models
    logger.info("Starting DeepFace service...")
    try:
        df = get_deepface()
        # Warm up the model with a dummy image
        dummy_img = np.zeros((48, 48, 3), dtype=np.uint8)
        df.analyze(dummy_img, actions=['emotion'], enforce_detection=False, silent=True)
        logger.info("DeepFace models pre-loaded successfully")
    except Exception as e:
        logger.warning(f"Model pre-loading skipped: {e}")

    yield

    # Shutdown
    logger.info("Shutting down DeepFace service...")


app = FastAPI(
    title="DeepFace Emotion Recognition Service",
    description="Microservice for facial expression and emotion detection using DeepFace",
    version="1.0.0",
    lifespan=lifespan
)

# CORS middleware for cross-origin requests
app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)


# Response models
class EmotionScore(BaseModel):
    """Individual emotion score"""
    emotion: str
    score: float


class FacialExpressionResponse(BaseModel):
    """Response model for facial expression detection"""
    face_detected: bool
    dominant_emotion: Optional[str] = None
    confidence: float = 0.0
    emotions: dict[str, float] = {}
    age: Optional[int] = None
    gender: Optional[str] = None
    race: Optional[str] = None


class EmotionalAnalysisResponse(BaseModel):
    """Response model for comprehensive emotional analysis"""
    primary_emotion: str
    emotion_scores: dict[str, float]
    intensity: float
    confidence: float
    sentiment: str  # Positive, Negative, Neutral
    sentiment_score: float
    arousal_level: float
    valence_level: float


class HealthResponse(BaseModel):
    """Health check response"""
    status: str
    service: str
    version: str
    deepface_loaded: bool


# Emotion to sentiment mapping
POSITIVE_EMOTIONS = ['happy', 'surprise']
NEGATIVE_EMOTIONS = ['angry', 'disgust', 'fear', 'sad']
NEUTRAL_EMOTIONS = ['neutral']

# Emotion to arousal/valence mapping (simplified circumplex model)
EMOTION_AROUSAL = {
    'angry': 0.8,
    'disgust': 0.4,
    'fear': 0.9,
    'happy': 0.7,
    'sad': 0.2,
    'surprise': 0.9,
    'neutral': 0.3
}

EMOTION_VALENCE = {
    'angry': 0.2,
    'disgust': 0.2,
    'fear': 0.2,
    'happy': 0.9,
    'sad': 0.1,
    'surprise': 0.6,
    'neutral': 0.5
}


def calculate_sentiment(emotions: dict[str, float]) -> tuple[str, float]:
    """Calculate overall sentiment from emotion scores"""
    positive_score = sum(emotions.get(e, 0) for e in POSITIVE_EMOTIONS)
    negative_score = sum(emotions.get(e, 0) for e in NEGATIVE_EMOTIONS)
    neutral_score = emotions.get('neutral', 0)

    total = positive_score + negative_score + neutral_score
    if total == 0:
        return 'Neutral', 0.5

    # Normalize scores
    positive_norm = positive_score / total
    negative_norm = negative_score / total

    # Calculate sentiment score (-1 to 1, mapped to 0 to 1)
    sentiment_score = (positive_norm - negative_norm + 1) / 2

    if positive_norm > negative_norm and positive_norm > neutral_score / total:
        return 'Positive', sentiment_score
    elif negative_norm > positive_norm and negative_norm > neutral_score / total:
        return 'Negative', sentiment_score
    else:
        return 'Neutral', sentiment_score


def calculate_arousal_valence(emotions: dict[str, float]) -> tuple[float, float]:
    """Calculate arousal and valence from emotion scores"""
    total_weight = sum(emotions.values())
    if total_weight == 0:
        return 0.5, 0.5

    arousal = sum(
        emotions.get(e, 0) * EMOTION_AROUSAL.get(e, 0.5)
        for e in emotions
    ) / total_weight

    valence = sum(
        emotions.get(e, 0) * EMOTION_VALENCE.get(e, 0.5)
        for e in emotions
    ) / total_weight

    return arousal, valence


@app.get("/health", response_model=HealthResponse)
async def health_check():
    """Health check endpoint"""
    return HealthResponse(
        status="healthy",
        service="deepface-emotion-service",
        version="1.0.0",
        deepface_loaded=deepface is not None
    )


@app.post("/analyze/facial-expression", response_model=FacialExpressionResponse)
async def analyze_facial_expression(
    file: UploadFile = File(...),
    include_demographics: bool = False
):
    """
    Analyze facial expression from an uploaded image.

    Args:
        file: Image file (JPEG, PNG, etc.)
        include_demographics: Whether to include age, gender, race analysis

    Returns:
        FacialExpressionResponse with detected emotions and confidence
    """
    try:
        # Read and validate image
        contents = await file.read()
        image = Image.open(io.BytesIO(contents))

        # Convert to RGB if necessary
        if image.mode != 'RGB':
            image = image.convert('RGB')

        # Convert to numpy array
        img_array = np.array(image)

        # Determine actions to perform
        actions = ['emotion']
        if include_demographics:
            actions.extend(['age', 'gender', 'race'])

        # Run DeepFace analysis
        df = get_deepface()
        results = df.analyze(
            img_array,
            actions=actions,
            enforce_detection=False,
            silent=True
        )

        # Handle single or multiple face results
        if isinstance(results, list):
            if len(results) == 0:
                return FacialExpressionResponse(face_detected=False)
            result = results[0]  # Take first detected face
        else:
            result = results

        # Check if face was detected
        face_detected = result.get('face_confidence', 0) > 0.5 if 'face_confidence' in result else True

        # Extract emotion data
        emotions = result.get('emotion', {})
        dominant_emotion = result.get('dominant_emotion', 'neutral')
        confidence = emotions.get(dominant_emotion, 0) / 100.0

        # Normalize emotion scores to 0-1 range
        normalized_emotions = {k: v / 100.0 for k, v in emotions.items()}

        response = FacialExpressionResponse(
            face_detected=face_detected,
            dominant_emotion=dominant_emotion,
            confidence=confidence,
            emotions=normalized_emotions
        )

        # Add demographics if requested
        if include_demographics:
            response.age = result.get('age')
            response.gender = result.get('dominant_gender')
            response.race = result.get('dominant_race')

        return response

    except Exception as e:
        logger.error(f"Error analyzing facial expression: {e}")
        raise HTTPException(status_code=500, detail=f"Analysis failed: {str(e)}")


@app.post("/analyze/emotion", response_model=EmotionalAnalysisResponse)
async def analyze_emotion(file: UploadFile = File(...)):
    """
    Comprehensive emotional analysis from facial expression.

    Provides detailed emotion scores, sentiment analysis, and
    arousal/valence levels based on the circumplex model of affect.

    Args:
        file: Image file (JPEG, PNG, etc.)

    Returns:
        EmotionalAnalysisResponse with comprehensive emotional metrics
    """
    try:
        # Read and validate image
        contents = await file.read()
        image = Image.open(io.BytesIO(contents))

        # Convert to RGB if necessary
        if image.mode != 'RGB':
            image = image.convert('RGB')

        # Convert to numpy array
        img_array = np.array(image)

        # Run DeepFace analysis
        df = get_deepface()
        results = df.analyze(
            img_array,
            actions=['emotion'],
            enforce_detection=False,
            silent=True
        )

        # Handle single or multiple face results
        if isinstance(results, list):
            if len(results) == 0:
                # Return neutral if no face detected
                return EmotionalAnalysisResponse(
                    primary_emotion='neutral',
                    emotion_scores={'neutral': 1.0},
                    intensity=0.0,
                    confidence=0.0,
                    sentiment='Neutral',
                    sentiment_score=0.5,
                    arousal_level=0.3,
                    valence_level=0.5
                )
            result = results[0]
        else:
            result = results

        # Extract and normalize emotion data
        emotions = result.get('emotion', {})
        normalized_emotions = {k: v / 100.0 for k, v in emotions.items()}

        dominant_emotion = result.get('dominant_emotion', 'neutral')
        confidence = normalized_emotions.get(dominant_emotion, 0)

        # Calculate intensity (how strong the dominant emotion is)
        max_score = max(normalized_emotions.values()) if normalized_emotions else 0
        intensity = max_score

        # Calculate sentiment
        sentiment, sentiment_score = calculate_sentiment(normalized_emotions)

        # Calculate arousal and valence
        arousal, valence = calculate_arousal_valence(normalized_emotions)

        return EmotionalAnalysisResponse(
            primary_emotion=dominant_emotion,
            emotion_scores=normalized_emotions,
            intensity=intensity,
            confidence=confidence,
            sentiment=sentiment,
            sentiment_score=sentiment_score,
            arousal_level=arousal,
            valence_level=valence
        )

    except Exception as e:
        logger.error(f"Error in emotional analysis: {e}")
        raise HTTPException(status_code=500, detail=f"Analysis failed: {str(e)}")


@app.post("/analyze/batch")
async def analyze_batch(files: list[UploadFile] = File(...)):
    """
    Batch analyze multiple images for emotions.

    Args:
        files: List of image files

    Returns:
        List of EmotionalAnalysisResponse for each image
    """
    results = []
    for file in files:
        try:
            # Rewind file if needed
            await file.seek(0)
            result = await analyze_emotion(file)
            results.append({"filename": file.filename, "analysis": result})
        except Exception as e:
            results.append({"filename": file.filename, "error": str(e)})

    return results


if __name__ == "__main__":
    import uvicorn
    uvicorn.run(app, host="0.0.0.0", port=8001)
