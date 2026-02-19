"""Embedding provider factory."""

import logging
import os

from .base import EmbeddingProvider
from .openai_embeddings import OpenAIEmbeddingProvider
from .google_embeddings import GoogleEmbeddingProvider
from .local_embeddings import LocalEmbeddingProvider

logger = logging.getLogger(__name__)

_PROVIDERS = {
    "openai": OpenAIEmbeddingProvider,
    "google": GoogleEmbeddingProvider,
    "local": LocalEmbeddingProvider,
}


def create_embedding_provider() -> EmbeddingProvider:
    """Create an embedding provider based on EMBEDDING_PROVIDER env var."""
    provider_name = os.getenv("EMBEDDING_PROVIDER", "openai").lower()

    if provider_name not in _PROVIDERS:
        logger.warning(
            f"Unknown EMBEDDING_PROVIDER '{provider_name}', falling back to openai"
        )
        provider_name = "openai"

    provider = _PROVIDERS[provider_name]()

    if provider_name != "openai":
        logger.warning(
            f"Embedding provider '{provider_name}' produces {provider.dimension}-dim vectors. "
            f"Existing DB column is vector(1536) — ensure dimensions match your schema."
        )

    logger.info(
        f"Embedding provider: {provider.provider_name}, dimension: {provider.dimension}, "
        f"available: {provider.is_available()}"
    )
    return provider


__all__ = [
    "EmbeddingProvider",
    "OpenAIEmbeddingProvider",
    "GoogleEmbeddingProvider",
    "LocalEmbeddingProvider",
    "create_embedding_provider",
]
