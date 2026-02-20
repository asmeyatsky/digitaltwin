"""Shared fixtures for LLM service tests."""

import os
from unittest.mock import AsyncMock, MagicMock

import pytest
from httpx import ASGITransport, AsyncClient


@pytest.fixture(autouse=True)
def _clean_env(monkeypatch):
    """Ensure no real API keys leak into tests."""
    for key in ("OPENAI_API_KEY", "ANTHROPIC_API_KEY", "GOOGLE_API_KEY", "SERVICE_API_KEY"):
        monkeypatch.delenv(key, raising=False)


@pytest.fixture
def mock_llm_provider():
    provider = MagicMock()
    provider.provider_name = "mock"
    provider.model = "mock-model"
    provider.is_available.return_value = True
    provider.generate_chat_response = AsyncMock(return_value="Mock LLM response")
    provider.classify_emotion = AsyncMock(
        return_value={"emotion": "happy", "confidence": 0.9}
    )
    return provider


@pytest.fixture
def mock_embedding_provider():
    provider = MagicMock()
    provider.provider_name = "mock"
    provider.dimension = 1536
    provider.is_available.return_value = True
    provider.generate_embedding = AsyncMock(return_value=[0.1] * 1536)
    return provider


@pytest.fixture
def service_api_key(monkeypatch):
    """Set SERVICE_API_KEY and return the value."""
    key = "test-service-key"
    monkeypatch.setenv("SERVICE_API_KEY", key)
    return key


@pytest.fixture
def service_api_headers(service_api_key):
    """Headers dict with the service API key."""
    return {"X-Service-Key": service_api_key}


@pytest.fixture
async def test_client(mock_llm_provider, mock_embedding_provider, service_api_key):
    """AsyncClient wired to the FastAPI app with mocked providers."""
    import main

    # Patch module-level globals
    main.llm_provider = mock_llm_provider
    main.embedding_provider = mock_embedding_provider
    main.SERVICE_API_KEY = service_api_key

    transport = ASGITransport(app=main.app)
    async with AsyncClient(transport=transport, base_url="http://test") as client:
        yield client

    # Restore
    main.llm_provider = None
    main.embedding_provider = None
    main.SERVICE_API_KEY = ""
