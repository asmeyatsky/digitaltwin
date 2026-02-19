"""LLM provider factory."""

import logging
import os

from .base import LLMProvider
from .openai_provider import OpenAIProvider
from .anthropic_provider import AnthropicProvider
from .google_provider import GoogleProvider

logger = logging.getLogger(__name__)

_PROVIDERS = {
    "openai": OpenAIProvider,
    "anthropic": AnthropicProvider,
    "google": GoogleProvider,
}


def create_llm_provider() -> LLMProvider:
    """Create an LLM provider based on LLM_PROVIDER and LLM_MODEL env vars."""
    provider_name = os.getenv("LLM_PROVIDER", "openai").lower()
    model = os.getenv("LLM_MODEL", "").strip() or None

    if provider_name not in _PROVIDERS:
        logger.warning(
            f"Unknown LLM_PROVIDER '{provider_name}', falling back to openai"
        )
        provider_name = "openai"

    provider = _PROVIDERS[provider_name](model=model)
    logger.info(
        f"LLM provider: {provider.provider_name}, model: {provider.model}, "
        f"available: {provider.is_available()}"
    )
    return provider


__all__ = [
    "LLMProvider",
    "OpenAIProvider",
    "AnthropicProvider",
    "GoogleProvider",
    "create_llm_provider",
]
