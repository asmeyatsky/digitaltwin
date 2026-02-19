"""Abstract base class for embedding providers."""

from abc import ABC, abstractmethod


class EmbeddingProvider(ABC):
    """Base class that all embedding providers must implement."""

    provider_name: str
    dimension: int

    @abstractmethod
    async def generate_embedding(self, text: str) -> list[float]:
        """Generate an embedding vector for the given text."""
        ...

    @abstractmethod
    def is_available(self) -> bool:
        """Check whether this provider is configured and usable."""
        ...
