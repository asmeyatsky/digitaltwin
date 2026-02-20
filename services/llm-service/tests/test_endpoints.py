"""Tests for FastAPI endpoints in main.py."""

from unittest.mock import AsyncMock, MagicMock

import pytest


class TestHealthEndpoint:
    async def test_health_shape(self, test_client):
        resp = await test_client.get("/health")
        assert resp.status_code == 200
        data = resp.json()
        assert data["status"] == "healthy"
        assert data["service"] == "llm-service"
        assert data["version"] == "1.0.0"
        assert "model" in data
        assert "llm_provider" in data
        assert "embedding_provider" in data
        assert "embedding_dimension" in data

    async def test_health_no_auth_required(self, test_client):
        """Health endpoint should not require X-Service-Key."""
        resp = await test_client.get("/health")
        assert resp.status_code == 200


class TestGenerateResponse:
    async def test_success(self, test_client, service_api_headers, mock_llm_provider):
        resp = await test_client.post(
            "/generate-response",
            json={"message": "I feel great today", "emotion": "happy"},
            headers=service_api_headers,
        )
        assert resp.status_code == 200
        data = resp.json()
        assert "response" in data
        assert "emotion" in data
        assert "confidence" in data
        assert "timestamp" in data

    async def test_mock_fallback_when_unavailable(
        self, test_client, service_api_headers, mock_llm_provider
    ):
        mock_llm_provider.is_available.return_value = False
        resp = await test_client.post(
            "/generate-response",
            json={"message": "hello", "emotion": "neutral"},
            headers=service_api_headers,
        )
        assert resp.status_code == 200
        data = resp.json()
        assert data["confidence"] == 0.6
        assert data["response_type"] == "mock_response"

    async def test_provider_error_fallback(
        self, test_client, service_api_headers, mock_llm_provider
    ):
        mock_llm_provider.generate_chat_response.side_effect = RuntimeError("API down")
        resp = await test_client.post(
            "/generate-response",
            json={"message": "hello", "emotion": "neutral"},
            headers=service_api_headers,
        )
        assert resp.status_code == 200
        data = resp.json()
        assert data["response_type"] == "mock_response"

    async def test_emotion_mapping_happy(
        self, test_client, service_api_headers
    ):
        resp = await test_client.post(
            "/generate-response",
            json={"message": "great day", "emotion": "happy"},
            headers=service_api_headers,
        )
        assert resp.json()["emotion"] == "happy"

    async def test_emotion_mapping_sad(
        self, test_client, service_api_headers
    ):
        resp = await test_client.post(
            "/generate-response",
            json={"message": "bad day", "emotion": "sad"},
            headers=service_api_headers,
        )
        assert resp.json()["emotion"] == "supportive"

    async def test_emotion_mapping_anxious(
        self, test_client, service_api_headers
    ):
        resp = await test_client.post(
            "/generate-response",
            json={"message": "worried", "emotion": "anxious"},
            headers=service_api_headers,
        )
        assert resp.json()["emotion"] == "calming"

    async def test_401_without_api_key(self, test_client):
        resp = await test_client.post(
            "/generate-response",
            json={"message": "hello", "emotion": "neutral"},
        )
        assert resp.status_code == 401

    async def test_503_when_service_not_configured(
        self, mock_llm_provider, mock_embedding_provider
    ):
        """When SERVICE_API_KEY is empty, non-health endpoints return 503."""
        import main

        main.llm_provider = mock_llm_provider
        main.embedding_provider = mock_embedding_provider
        main.SERVICE_API_KEY = ""

        from httpx import ASGITransport, AsyncClient

        transport = ASGITransport(app=main.app)
        async with AsyncClient(transport=transport, base_url="http://test") as client:
            resp = await client.post(
                "/generate-response",
                json={"message": "hello", "emotion": "neutral"},
            )
            assert resp.status_code == 503


class TestAnalyzeMessage:
    async def test_success(self, test_client, service_api_headers):
        resp = await test_client.post(
            "/analyze-message",
            json={"message": "I love my work at the office"},
            headers=service_api_headers,
        )
        assert resp.status_code == 200
        data = resp.json()
        assert data["word_count"] == 7
        assert "positive: love" in data["emotional_indicators"]
        assert "work" in data["topics"]

    async def test_empty_message(self, test_client, service_api_headers):
        resp = await test_client.post(
            "/analyze-message",
            json={"message": ""},
            headers=service_api_headers,
        )
        assert resp.status_code == 200
        assert "error" in resp.json()

    async def test_urgency_high(self, test_client, service_api_headers):
        resp = await test_client.post(
            "/analyze-message",
            json={"message": "I hate this terrible situation"},
            headers=service_api_headers,
        )
        data = resp.json()
        assert data["urgency"] == "high"


class TestAnalyzeEmotionText:
    async def test_with_llm(self, test_client, service_api_headers, mock_llm_provider):
        mock_llm_provider.classify_emotion.return_value = {
            "emotion": "excited",
            "confidence": 0.88,
        }
        resp = await test_client.post(
            "/analyze-emotion-text",
            json={"text": "This is amazing!"},
            headers=service_api_headers,
        )
        assert resp.status_code == 200
        data = resp.json()
        assert data["emotion"] == "excited"
        assert data["confidence"] == 0.88

    async def test_fallback_when_unavailable(
        self, test_client, service_api_headers, mock_llm_provider
    ):
        mock_llm_provider.is_available.return_value = False
        resp = await test_client.post(
            "/analyze-emotion-text",
            json={"text": "I feel so happy and glad"},
            headers=service_api_headers,
        )
        data = resp.json()
        assert data["emotion"] == "happy"

    async def test_invalid_emotion_normalized(
        self, test_client, service_api_headers, mock_llm_provider
    ):
        mock_llm_provider.classify_emotion.return_value = {
            "emotion": "nostalgic",  # not in AD1_EMOTIONS
            "confidence": 0.7,
        }
        resp = await test_client.post(
            "/analyze-emotion-text",
            json={"text": "I miss those days"},
            headers=service_api_headers,
        )
        data = resp.json()
        assert data["emotion"] == "neutral"


class TestEmbeddingEndpoint:
    async def test_success(self, test_client, service_api_headers, mock_embedding_provider):
        resp = await test_client.post(
            "/embedding",
            json={"text": "hello world"},
            headers=service_api_headers,
        )
        assert resp.status_code == 200
        data = resp.json()
        assert len(data["embedding"]) == 1536

    async def test_unavailable_returns_503(
        self, test_client, service_api_headers, mock_embedding_provider
    ):
        mock_embedding_provider.is_available.return_value = False
        resp = await test_client.post(
            "/embedding",
            json={"text": "hello"},
            headers=service_api_headers,
        )
        assert resp.status_code == 503
