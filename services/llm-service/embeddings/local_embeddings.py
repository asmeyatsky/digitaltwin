"""Local embedding provider — sentence-transformers all-MiniLM-L6-v2 (384-dim)."""

import asyncio
import logging

from .base import EmbeddingProvider

logger = logging.getLogger(__name__)


class LocalEmbeddingProvider(EmbeddingProvider):
    provider_name = "local"
    dimension = 384

    def __init__(self):
        self._model = None

    def is_available(self) -> bool:
        try:
            import sentence_transformers  # noqa: F401
            return True
        except ImportError:
            return False

    def _get_model(self):
        if self._model is None:
            from sentence_transformers import SentenceTransformer
            self._model = SentenceTransformer("all-MiniLM-L6-v2")
            logger.info("Loaded local sentence-transformers model: all-MiniLM-L6-v2")
        return self._model

    async def generate_embedding(self, text: str) -> list[float]:
        model = self._get_model()
        embedding = await asyncio.to_thread(model.encode, text)
        return embedding.tolist()
