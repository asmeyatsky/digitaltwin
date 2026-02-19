"""OpenAI embedding provider — text-embedding-3-small (1536-dim)."""

import logging
import os

import openai

from .base import EmbeddingProvider

logger = logging.getLogger(__name__)


class OpenAIEmbeddingProvider(EmbeddingProvider):
    provider_name = "openai"
    dimension = 1536

    def __init__(self):
        self._client: openai.AsyncOpenAI | None = None

    def is_available(self) -> bool:
        return bool(os.getenv("OPENAI_API_KEY"))

    def _get_client(self) -> openai.AsyncOpenAI:
        if self._client is None:
            self._client = openai.AsyncOpenAI()
        return self._client

    async def generate_embedding(self, text: str) -> list[float]:
        client = self._get_client()
        response = await client.embeddings.create(
            model="text-embedding-3-small",
            input=text,
        )
        return response.data[0].embedding
