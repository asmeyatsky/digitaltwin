"""
Voice Service with ElevenLabs Integration
Provides text-to-speech and voice cloning capabilities for the Avatar Twin
"""

import os
import io
import uuid
import logging
import asyncio
from typing import Optional
from contextlib import asynccontextmanager

import httpx
from fastapi import FastAPI, HTTPException, UploadFile, File, Form, BackgroundTasks, Request
from fastapi.middleware.cors import CORSMiddleware
from fastapi.responses import StreamingResponse
from starlette.responses import JSONResponse
from pydantic import BaseModel
from slowapi import Limiter, _rate_limit_exceeded_handler
from slowapi.util import get_remote_address
from slowapi.errors import RateLimitExceeded

# Configure logging
logging.basicConfig(level=logging.INFO)
logger = logging.getLogger(__name__)

# ElevenLabs configuration
ELEVENLABS_API_KEY = os.getenv("ELEVENLABS_API_KEY", "")
ELEVENLABS_BASE_URL = "https://api.elevenlabs.io/v1"

# Voice storage
VOICE_STORAGE_PATH = os.getenv("VOICE_STORAGE_PATH", "/app/voices")
os.makedirs(VOICE_STORAGE_PATH, exist_ok=True)

# In-memory storage (use database in production)
user_voices = {}  # user_id -> voice_id mapping
voice_cloning_jobs = {}

# File upload validation constants
ALLOWED_AUDIO_EXTENSIONS = {".mp3", ".wav", ".ogg", ".flac", ".m4a", ".aac", ".wma", ".webm"}
ALLOWED_AUDIO_MIMES = {
    "audio/mpeg", "audio/wav", "audio/x-wav", "audio/ogg", "audio/flac",
    "audio/mp4", "audio/aac", "audio/x-m4a", "audio/webm",
}
MAX_AUDIO_SIZE = 50 * 1024 * 1024  # 50MB


async def validate_audio_upload(file: UploadFile) -> bytes:
    """Validate and read an audio upload. Returns file contents."""
    # Check extension
    filename = (file.filename or "").lower()
    ext = os.path.splitext(filename)[1]
    if ext and ext not in ALLOWED_AUDIO_EXTENSIONS:
        raise HTTPException(status_code=400, detail=f"Invalid file extension: {ext}. Allowed: {ALLOWED_AUDIO_EXTENSIONS}")

    # Check MIME type
    content_type = (file.content_type or "").lower()
    if content_type and content_type not in ALLOWED_AUDIO_MIMES:
        raise HTTPException(status_code=400, detail=f"Invalid content type: {content_type}. Allowed: {ALLOWED_AUDIO_MIMES}")

    # Read with size limit
    contents = await file.read()
    if len(contents) > MAX_AUDIO_SIZE:
        raise HTTPException(status_code=400, detail=f"File too large: {len(contents)} bytes. Maximum: {MAX_AUDIO_SIZE} bytes")

    return contents


SERVICE_API_KEY = os.getenv("SERVICE_API_KEY", "")


@asynccontextmanager
async def lifespan(app: FastAPI):
    """Application lifespan handler"""
    logger.info("Starting Voice Service...")

    if not SERVICE_API_KEY:
        logger.warning("SERVICE_API_KEY is not set — all non-health endpoints will return 503")

    if not ELEVENLABS_API_KEY:
        logger.warning("ELEVENLABS_API_KEY not set - voice features will be limited")
    yield
    logger.info("Shutting down Voice Service...")


app = FastAPI(
    title="Voice Service",
    description="Text-to-speech and voice cloning using ElevenLabs",
    version="1.0.0",
    lifespan=lifespan
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


app.add_middleware(
    CORSMiddleware,
    allow_origins=[o.strip() for o in os.getenv("CORS_ORIGINS", "http://localhost:8080").split(",")],
    allow_credentials=True,
    allow_methods=["GET", "POST", "DELETE"],
    allow_headers=["Content-Type", "Authorization", "X-Service-Key"],
)


# Models
class TTSRequest(BaseModel):
    """Text-to-speech request"""
    text: str
    voice_id: Optional[str] = None
    user_id: Optional[str] = None
    model_id: str = "eleven_multilingual_v2"
    stability: float = 0.5
    similarity_boost: float = 0.75
    style: float = 0.0
    use_speaker_boost: bool = True


class VoiceCloneRequest(BaseModel):
    """Voice cloning request"""
    user_id: str
    voice_name: str
    description: Optional[str] = None


class VoiceInfo(BaseModel):
    """Voice information"""
    voice_id: str
    name: str
    category: str
    description: Optional[str] = None
    preview_url: Optional[str] = None
    labels: dict = {}


class VoiceCloneStatus(BaseModel):
    """Voice cloning job status"""
    job_id: str
    user_id: str
    status: str  # pending, processing, completed, failed
    voice_id: Optional[str] = None
    error: Optional[str] = None


class VisemeData(BaseModel):
    """Viseme data for lip sync"""
    visemes: list[dict]
    duration_ms: int
    audio_url: Optional[str] = None


# Default voice IDs from ElevenLabs (free tier available)
DEFAULT_VOICES = {
    "male_1": "pNInz6obpgDQGcFmaJgB",  # Adam
    "male_2": "ErXwobaYiN019PkySvjV",  # Antoni
    "female_1": "EXAVITQu4vr4xnSDxMaL",  # Bella
    "female_2": "MF3mGyEYCl7XYWbV9V6O",  # Elli
    "neutral": "TxGEqnHWrfWFTfGW9XjX"  # Josh
}


def get_headers():
    """Get API headers"""
    return {
        "xi-api-key": ELEVENLABS_API_KEY,
        "Content-Type": "application/json"
    }


@app.get("/health")
async def health_check():
    """Health check endpoint"""
    return {
        "status": "healthy",
        "service": "voice-service",
        "version": "1.0.0",
        "elevenlabs_configured": bool(ELEVENLABS_API_KEY)
    }


@app.get("/voices", response_model=list[VoiceInfo])
@limiter.limit("60/minute")
async def list_voices(request: Request):
    """List all available voices from ElevenLabs"""
    if not ELEVENLABS_API_KEY:
        # Return default voice list without API
        return [
            VoiceInfo(voice_id=v, name=k, category="premade")
            for k, v in DEFAULT_VOICES.items()
        ]

    try:
        async with httpx.AsyncClient() as client:
            response = await client.get(
                f"{ELEVENLABS_BASE_URL}/voices",
                headers=get_headers()
            )
            response.raise_for_status()
            data = response.json()

            voices = []
            for voice in data.get("voices", []):
                voices.append(VoiceInfo(
                    voice_id=voice["voice_id"],
                    name=voice["name"],
                    category=voice.get("category", "unknown"),
                    description=voice.get("description"),
                    preview_url=voice.get("preview_url"),
                    labels=voice.get("labels", {})
                ))
            return voices

    except httpx.HTTPError as e:
        logger.error(f"Error fetching voices: {e}")
        raise HTTPException(status_code=503, detail="ElevenLabs API unavailable")


@app.post("/tts")
@limiter.limit("60/minute")
async def text_to_speech(request: Request, tts_request: TTSRequest):
    """
    Convert text to speech using ElevenLabs.

    Returns audio stream (MP3 format).
    """
    if not ELEVENLABS_API_KEY:
        raise HTTPException(status_code=503, detail="ElevenLabs API key not configured")

    # Determine voice to use
    voice_id = tts_request.voice_id
    if not voice_id and tts_request.user_id:
        # Check if user has a cloned voice
        voice_id = user_voices.get(tts_request.user_id)
    if not voice_id:
        voice_id = DEFAULT_VOICES["neutral"]

    try:
        async with httpx.AsyncClient() as client:
            response = await client.post(
                f"{ELEVENLABS_BASE_URL}/text-to-speech/{voice_id}",
                headers=get_headers(),
                json={
                    "text": tts_request.text,
                    "model_id": tts_request.model_id,
                    "voice_settings": {
                        "stability": tts_request.stability,
                        "similarity_boost": tts_request.similarity_boost,
                        "style": tts_request.style,
                        "use_speaker_boost": tts_request.use_speaker_boost
                    }
                },
                timeout=60.0
            )
            response.raise_for_status()

            # Return audio stream
            return StreamingResponse(
                io.BytesIO(response.content),
                media_type="audio/mpeg",
                headers={"Content-Disposition": "attachment; filename=speech.mp3"}
            )

    except httpx.HTTPError as e:
        logger.error(f"TTS error: {e}")
        raise HTTPException(status_code=503, detail="Internal server error")


@app.post("/tts/with-visemes")
@limiter.limit("30/minute")
async def text_to_speech_with_visemes(request: Request, tts_request: TTSRequest):
    """
    Convert text to speech and return viseme data for lip sync.

    Returns both audio and timing data for mouth animations.
    """
    if not ELEVENLABS_API_KEY:
        raise HTTPException(status_code=503, detail="ElevenLabs API key not configured")

    voice_id = tts_request.voice_id
    if not voice_id and tts_request.user_id:
        voice_id = user_voices.get(tts_request.user_id)
    if not voice_id:
        voice_id = DEFAULT_VOICES["neutral"]

    try:
        async with httpx.AsyncClient() as client:
            # Request with timestamps for viseme generation
            response = await client.post(
                f"{ELEVENLABS_BASE_URL}/text-to-speech/{voice_id}/with-timestamps",
                headers=get_headers(),
                json={
                    "text": tts_request.text,
                    "model_id": tts_request.model_id,
                    "voice_settings": {
                        "stability": tts_request.stability,
                        "similarity_boost": tts_request.similarity_boost,
                        "style": tts_request.style,
                        "use_speaker_boost": tts_request.use_speaker_boost
                    }
                },
                timeout=60.0
            )
            response.raise_for_status()
            data = response.json()

            # Extract timing information and convert to visemes
            audio_base64 = data.get("audio_base64", "")
            alignment = data.get("alignment", {})

            # Generate viseme data from alignment
            visemes = generate_visemes_from_alignment(alignment, tts_request.text)

            # Save audio temporarily
            audio_id = str(uuid.uuid4())
            audio_path = os.path.join(VOICE_STORAGE_PATH, f"{audio_id}.mp3")

            import base64
            audio_bytes = base64.b64decode(audio_base64)
            with open(audio_path, 'wb') as f:
                f.write(audio_bytes)

            return {
                "audio_url": f"/audio/{audio_id}",
                "visemes": visemes,
                "duration_ms": calculate_audio_duration(alignment),
                "text": tts_request.text
            }

    except httpx.HTTPError as e:
        logger.error(f"TTS with visemes error: {e}")

        # Fallback: generate without timestamps
        try:
            # Get regular TTS
            async with httpx.AsyncClient() as client:
                response = await client.post(
                    f"{ELEVENLABS_BASE_URL}/text-to-speech/{voice_id}",
                    headers=get_headers(),
                    json={
                        "text": tts_request.text,
                        "model_id": tts_request.model_id,
                        "voice_settings": {
                            "stability": tts_request.stability,
                            "similarity_boost": tts_request.similarity_boost
                        }
                    },
                    timeout=60.0
                )
                response.raise_for_status()

                # Save audio
                audio_id = str(uuid.uuid4())
                audio_path = os.path.join(VOICE_STORAGE_PATH, f"{audio_id}.mp3")
                with open(audio_path, 'wb') as f:
                    f.write(response.content)

                # Generate estimated visemes
                visemes = generate_estimated_visemes(tts_request.text)

                return {
                    "audio_url": f"/audio/{audio_id}",
                    "visemes": visemes,
                    "duration_ms": estimate_speech_duration(tts_request.text),
                    "text": tts_request.text
                }

        except Exception as fallback_error:
            logger.error(f"Fallback TTS also failed: {fallback_error}")
            raise HTTPException(status_code=503, detail="Internal server error")


@app.get("/audio/{audio_id}")
@limiter.limit("60/minute")
async def get_audio(request: Request, audio_id: str):
    """Download generated audio file"""
    audio_path = os.path.join(VOICE_STORAGE_PATH, f"{audio_id}.mp3")
    if not os.path.exists(audio_path):
        raise HTTPException(status_code=404, detail="Audio not found")

    def iterfile():
        with open(audio_path, 'rb') as f:
            yield from f

    return StreamingResponse(iterfile(), media_type="audio/mpeg")


@app.post("/voice/clone", response_model=VoiceCloneStatus)
@limiter.limit("5/minute")
async def clone_voice(
    request: Request,
    user_id: str = Form(...),
    voice_name: str = Form(...),
    description: str = Form(None),
    files: list[UploadFile] = File(...),
    background_tasks: BackgroundTasks = None
):
    """
    Clone a user's voice from audio samples.

    Requires at least 1 minute of clear speech audio.
    Multiple files can be uploaded for better quality.
    """
    if not ELEVENLABS_API_KEY:
        raise HTTPException(status_code=503, detail="ElevenLabs API key not configured")

    if len(files) == 0:
        raise HTTPException(status_code=400, detail="At least one audio file required")

    job_id = str(uuid.uuid4())
    voice_cloning_jobs[job_id] = {
        "user_id": user_id,
        "status": "processing",
        "voice_id": None,
        "error": None
    }

    try:
        # Validate and prepare files for upload
        files_data = []
        for file in files:
            content = await validate_audio_upload(file)
            files_data.append(("files", (file.filename, content, file.content_type)))

        # Add voice clone to ElevenLabs
        async with httpx.AsyncClient() as client:
            response = await client.post(
                f"{ELEVENLABS_BASE_URL}/voices/add",
                headers={"xi-api-key": ELEVENLABS_API_KEY},
                data={
                    "name": voice_name,
                    "description": description or f"Cloned voice for user {user_id}"
                },
                files=files_data,
                timeout=120.0
            )
            response.raise_for_status()
            data = response.json()

            voice_id = data.get("voice_id")
            if voice_id:
                # Store mapping
                user_voices[user_id] = voice_id
                voice_cloning_jobs[job_id]["status"] = "completed"
                voice_cloning_jobs[job_id]["voice_id"] = voice_id

                logger.info(f"Voice cloned successfully: {voice_id} for user {user_id}")

                return VoiceCloneStatus(
                    job_id=job_id,
                    user_id=user_id,
                    status="completed",
                    voice_id=voice_id
                )
            else:
                raise HTTPException(status_code=500, detail="Voice cloning failed - no voice ID returned")

    except httpx.HTTPError as e:
        logger.error(f"Voice cloning error: {e}")
        voice_cloning_jobs[job_id]["status"] = "failed"
        voice_cloning_jobs[job_id]["error"] = str(e)
        raise HTTPException(status_code=503, detail="Internal server error")


@app.get("/voice/clone/{job_id}/status", response_model=VoiceCloneStatus)
@limiter.limit("60/minute")
async def get_clone_status(request: Request, job_id: str):
    """Get voice cloning job status"""
    if job_id not in voice_cloning_jobs:
        raise HTTPException(status_code=404, detail="Job not found")

    job = voice_cloning_jobs[job_id]
    return VoiceCloneStatus(
        job_id=job_id,
        user_id=job["user_id"],
        status=job["status"],
        voice_id=job.get("voice_id"),
        error=job.get("error")
    )


@app.get("/voice/user/{user_id}")
@limiter.limit("60/minute")
async def get_user_voice(request: Request, user_id: str):
    """Get the voice ID associated with a user"""
    voice_id = user_voices.get(user_id)
    if not voice_id:
        raise HTTPException(status_code=404, detail="No voice found for user")

    return {"user_id": user_id, "voice_id": voice_id}


class VoiceEmotionResult(BaseModel):
    """Voice emotion analysis result"""
    emotion: str
    confidence: float
    features: dict


@app.post("/voice/analyze-emotion", response_model=VoiceEmotionResult)
@limiter.limit("30/minute")
async def analyze_voice_emotion(request: Request, file: UploadFile = File(...)):
    """
    Analyze emotion from audio file using acoustic features.
    Accepts WAV or MP3 files. Extracts pitch, energy, speaking rate, and MFCCs
    to classify emotion via rule-based mapping.
    """
    try:
        import numpy as np

        content = await validate_audio_upload(file)

        # Write to temp file for librosa processing
        import tempfile
        with tempfile.NamedTemporaryFile(suffix=".wav", delete=False) as tmp:
            tmp.write(content)
            tmp_path = tmp.name

        try:
            import librosa

            # Load audio
            y, sr = librosa.load(tmp_path, sr=22050)

            if len(y) < sr * 0.5:  # Less than 0.5 seconds
                raise HTTPException(status_code=400, detail="Audio too short for analysis (minimum 0.5 seconds)")

            # Extract features
            # Pitch (fundamental frequency)
            pitches, magnitudes = librosa.piptrack(y=y, sr=sr)
            pitch_values = []
            for t in range(pitches.shape[1]):
                index = magnitudes[:, t].argmax()
                pitch = pitches[index, t]
                if pitch > 0:
                    pitch_values.append(pitch)

            pitch_mean = float(np.mean(pitch_values)) if pitch_values else 0.0
            pitch_std = float(np.std(pitch_values)) if pitch_values else 0.0

            # Energy (RMS)
            rms = librosa.feature.rms(y=y)[0]
            energy_mean = float(np.mean(rms))

            # Speaking rate (onset detection as proxy)
            onset_env = librosa.onset.onset_strength(y=y, sr=sr)
            onsets = librosa.onset.onset_detect(onset_envelope=onset_env, sr=sr)
            duration = len(y) / sr
            speaking_rate = len(onsets) / duration if duration > 0 else 0.0

            # MFCCs
            mfccs = librosa.feature.mfcc(y=y, sr=sr, n_mfcc=13)
            mfcc_mean = float(np.mean(mfccs))

            features = {
                "pitch_mean": round(pitch_mean, 2),
                "pitch_std": round(pitch_std, 2),
                "energy_mean": round(energy_mean, 6),
                "speaking_rate": round(speaking_rate, 2),
                "mfcc_mean": round(mfcc_mean, 4),
            }

            # Rule-based emotion classification
            emotion, confidence = _classify_voice_emotion(
                pitch_mean, pitch_std, energy_mean, speaking_rate
            )

            return VoiceEmotionResult(
                emotion=emotion,
                confidence=round(confidence, 2),
                features=features,
            )

        finally:
            os.unlink(tmp_path)

    except HTTPException:
        raise
    except Exception as e:
        logger.error(f"Voice emotion analysis error: {e}")
        raise HTTPException(status_code=500, detail="Voice emotion analysis failed")


def _classify_voice_emotion(
    pitch_mean: float, pitch_std: float, energy_mean: float, speaking_rate: float
) -> tuple[str, float]:
    """Rule-based emotion classification from acoustic features."""
    # Normalize features to rough 0-1 scale
    pitch_norm = min(pitch_mean / 500.0, 1.0)
    energy_norm = min(energy_mean / 0.1, 1.0)
    pitch_var_norm = min(pitch_std / 200.0, 1.0)

    # AD-1 compliant: all 8 unified emotions
    scores = {
        "happy": 0.0,
        "sad": 0.0,
        "angry": 0.0,
        "anxious": 0.0,
        "calm": 0.0,
        "surprised": 0.0,
        "excited": 0.0,
        "neutral": 0.3,
    }

    # High pitch + high energy → Excited/Happy
    if pitch_norm > 0.5 and energy_norm > 0.5:
        scores["happy"] = 0.4 + 0.2 * pitch_norm + 0.2 * energy_norm
        scores["excited"] = 0.3 + 0.3 * pitch_norm + 0.3 * energy_norm

    # Low pitch + low energy → Sad/Calm
    if pitch_norm < 0.4 and energy_norm < 0.4:
        scores["sad"] = 0.4 + 0.3 * (1 - pitch_norm) + 0.2 * (1 - energy_norm)
        scores["calm"] = 0.3 + 0.2 * (1 - energy_norm)

    # High pitch variance → Anxious/Surprised
    if pitch_var_norm > 0.5:
        scores["anxious"] = 0.3 + 0.3 * pitch_var_norm
        scores["surprised"] = 0.3 + 0.4 * pitch_var_norm

    # High energy + low pitch → Angry
    if energy_norm > 0.6 and pitch_norm < 0.4:
        scores["angry"] = 0.5 + 0.3 * energy_norm + 0.2 * (1 - pitch_norm)

    best_emotion = max(scores, key=scores.get)
    confidence = min(scores[best_emotion], 0.95)

    return best_emotion, confidence


@app.delete("/voice/user/{user_id}")
@limiter.limit("30/minute")
async def delete_user_voice(request: Request, user_id: str):
    """Delete a user's cloned voice"""
    voice_id = user_voices.get(user_id)
    if not voice_id:
        raise HTTPException(status_code=404, detail="No voice found for user")

    try:
        # Delete from ElevenLabs
        async with httpx.AsyncClient() as client:
            response = await client.delete(
                f"{ELEVENLABS_BASE_URL}/voices/{voice_id}",
                headers=get_headers()
            )
            # Don't raise on 404 - voice may already be deleted
            if response.status_code not in [200, 404]:
                response.raise_for_status()

        # Remove local mapping
        del user_voices[user_id]
        return {"status": "deleted", "user_id": user_id, "voice_id": voice_id}

    except httpx.HTTPError as e:
        logger.error(f"Error deleting voice: {e}")
        raise HTTPException(status_code=503, detail="Internal server error")


# Viseme generation helpers

# Phoneme to viseme mapping (simplified)
PHONEME_VISEME_MAP = {
    # Bilabial (lips together)
    'p': 'bilabial', 'b': 'bilabial', 'm': 'bilabial',
    # Labiodental (teeth on lip)
    'f': 'labiodental', 'v': 'labiodental',
    # Dental
    'th': 'dental',
    # Alveolar
    't': 'alveolar', 'd': 'alveolar', 'n': 'alveolar', 's': 'alveolar', 'z': 'alveolar', 'l': 'alveolar',
    # Postalveolar
    'sh': 'postalveolar', 'zh': 'postalveolar', 'ch': 'postalveolar', 'j': 'postalveolar',
    # Velar
    'k': 'velar', 'g': 'velar', 'ng': 'velar',
    # Vowels
    'a': 'open', 'e': 'mid', 'i': 'narrow', 'o': 'rounded', 'u': 'rounded',
    # Default
    ' ': 'rest'
}

# Viseme shapes for Unity blend shapes
VISEME_SHAPES = {
    'rest': {'jawOpen': 0.0, 'mouthSmile': 0.0, 'mouthPucker': 0.0},
    'bilabial': {'jawOpen': 0.1, 'mouthSmile': 0.0, 'mouthPucker': 0.3},
    'labiodental': {'jawOpen': 0.1, 'mouthSmile': 0.0, 'mouthPucker': 0.1},
    'dental': {'jawOpen': 0.2, 'mouthSmile': 0.2, 'mouthPucker': 0.0},
    'alveolar': {'jawOpen': 0.2, 'mouthSmile': 0.1, 'mouthPucker': 0.0},
    'postalveolar': {'jawOpen': 0.2, 'mouthSmile': 0.0, 'mouthPucker': 0.2},
    'velar': {'jawOpen': 0.3, 'mouthSmile': 0.0, 'mouthPucker': 0.0},
    'open': {'jawOpen': 0.6, 'mouthSmile': 0.1, 'mouthPucker': 0.0},
    'mid': {'jawOpen': 0.4, 'mouthSmile': 0.2, 'mouthPucker': 0.0},
    'narrow': {'jawOpen': 0.2, 'mouthSmile': 0.3, 'mouthPucker': 0.0},
    'rounded': {'jawOpen': 0.3, 'mouthSmile': 0.0, 'mouthPucker': 0.4}
}


def generate_visemes_from_alignment(alignment: dict, text: str) -> list:
    """Generate viseme data from ElevenLabs alignment data"""
    visemes = []
    characters = alignment.get("characters", [])
    character_start_times = alignment.get("character_start_times_seconds", [])
    character_end_times = alignment.get("character_end_times_seconds", [])

    for i, char in enumerate(characters):
        if i < len(character_start_times) and i < len(character_end_times):
            char_lower = char.lower()

            # Find matching viseme
            viseme_type = 'rest'
            for phoneme, viseme in PHONEME_VISEME_MAP.items():
                if char_lower == phoneme or char_lower.startswith(phoneme):
                    viseme_type = viseme
                    break

            shape = VISEME_SHAPES.get(viseme_type, VISEME_SHAPES['rest'])

            visemes.append({
                "character": char,
                "start_time_ms": int(character_start_times[i] * 1000),
                "end_time_ms": int(character_end_times[i] * 1000),
                "viseme_type": viseme_type,
                "blend_shapes": shape
            })

    return visemes


def generate_estimated_visemes(text: str) -> list:
    """Generate estimated visemes when alignment data is not available"""
    visemes = []
    avg_char_duration = 80  # Average ms per character

    current_time = 0
    for char in text:
        char_lower = char.lower()

        viseme_type = 'rest'
        for phoneme, viseme in PHONEME_VISEME_MAP.items():
            if char_lower == phoneme:
                viseme_type = viseme
                break

        if char == ' ':
            viseme_type = 'rest'
        elif char_lower in 'aeiou':
            viseme_type = PHONEME_VISEME_MAP.get(char_lower, 'open')

        shape = VISEME_SHAPES.get(viseme_type, VISEME_SHAPES['rest'])
        duration = avg_char_duration if char != ' ' else 50

        visemes.append({
            "character": char,
            "start_time_ms": current_time,
            "end_time_ms": current_time + duration,
            "viseme_type": viseme_type,
            "blend_shapes": shape
        })

        current_time += duration

    return visemes


def calculate_audio_duration(alignment: dict) -> int:
    """Calculate audio duration from alignment data"""
    end_times = alignment.get("character_end_times_seconds", [])
    if end_times:
        return int(max(end_times) * 1000)
    return 0


def estimate_speech_duration(text: str) -> int:
    """Estimate speech duration based on text length"""
    # Average speaking rate: ~150 words per minute, ~5 chars per word
    # = ~750 chars per minute = ~80ms per char
    return len(text) * 80


if __name__ == "__main__":
    import uvicorn
    uvicorn.run(app, host="0.0.0.0", port=8003)
