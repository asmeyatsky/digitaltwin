"""Abstract base class for LLM providers."""

from abc import ABC, abstractmethod
from typing import Optional


class LLMProvider(ABC):
    """Base class that all LLM providers must implement."""

    provider_name: str
    model: str

    @abstractmethod
    async def generate_chat_response(
        self, system_prompt: str, user_message: str, context: str = ""
    ) -> str:
        """Generate a chat response given a system prompt, user message, and optional context."""
        ...

    @abstractmethod
    async def classify_emotion(self, text: str, emotions: list[str]) -> dict:
        """Classify text into one of the given emotions.

        Returns:
            {"emotion": str, "confidence": float}
        """
        ...

    @abstractmethod
    def is_available(self) -> bool:
        """Check whether this provider is configured and usable."""
        ...
