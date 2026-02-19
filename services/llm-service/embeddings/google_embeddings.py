"""Google embedding provider — text-embedding-004 (768-dim)."""

import logging
import os

import google.generativeai as genai

from .base import EmbeddingProvider

logger = logging.getLogger(__name__)


class GoogleEmbeddingProvider(EmbeddingProvider):
    provider_name = "google"
    dimension = 768

    def __init__(self):
        self._configured = False

    def is_available(self) -> bool:
        return bool(os.getenv("GOOGLE_API_KEY"))

    def _ensure_configured(self):
        if not self._configured:
            genai.configure(api_key=os.getenv("GOOGLE_API_KEY"))
            self._configured = True

    async def generate_embedding(self, text: str) -> list[float]:
        self._ensure_configured()
        result = genai.embed_content(
            model="models/text-embedding-004",
            content=text,
        )
        return result["embedding"]
