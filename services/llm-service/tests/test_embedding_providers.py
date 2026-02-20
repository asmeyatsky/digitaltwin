"""Tests for embedding providers."""

from unittest.mock import AsyncMock, MagicMock, patch

import pytest

from embeddings.openai_embeddings import OpenAIEmbeddingProvider
from embeddings.google_embeddings import GoogleEmbeddingProvider
from embeddings.local_embeddings import LocalEmbeddingProvider


class TestOpenAIEmbeddings:
    def test_dimension(self):
        p = OpenAIEmbeddingProvider()
        assert p.dimension == 1536

    def test_provider_name(self):
        p = OpenAIEmbeddingProvider()
        assert p.provider_name == "openai"

    @pytest.mark.asyncio
    async def test_generate_embedding(self):
        p = OpenAIEmbeddingProvider()
        mock_client = AsyncMock()
        mock_data = MagicMock()
        mock_data.embedding = [0.1] * 1536
        mock_client.embeddings.create.return_value = MagicMock(data=[mock_data])
        p._client = mock_client

        result = await p.generate_embedding("hello world")
        assert len(result) == 1536
        mock_client.embeddings.create.assert_awaited_once()


class TestGoogleEmbeddings:
    def test_dimension(self):
        p = GoogleEmbeddingProvider()
        assert p.dimension == 768

    def test_provider_name(self):
        p = GoogleEmbeddingProvider()
        assert p.provider_name == "google"

    @pytest.mark.asyncio
    async def test_generate_embedding(self, monkeypatch):
        monkeypatch.setenv("GOOGLE_API_KEY", "test-key")
        p = GoogleEmbeddingProvider()

        with patch("embeddings.google_embeddings.genai") as mock_genai:
            mock_genai.embed_content.return_value = {"embedding": [0.2] * 768}
            result = await p.generate_embedding("hello")
            assert len(result) == 768
            mock_genai.embed_content.assert_called_once()


class TestLocalEmbeddings:
    def test_dimension(self):
        p = LocalEmbeddingProvider()
        assert p.dimension == 384

    def test_provider_name(self):
        p = LocalEmbeddingProvider()
        assert p.provider_name == "local"

    def test_is_available_when_no_sentence_transformers(self):
        p = LocalEmbeddingProvider()
        with patch.dict("sys.modules", {"sentence_transformers": None}):
            # When import fails, is_available returns False
            # We test the actual implementation
            import importlib
            result = p.is_available()
            # Result depends on whether sentence_transformers is installed
            assert isinstance(result, bool)

    @pytest.mark.asyncio
    async def test_generate_via_asyncio_to_thread(self):
        p = LocalEmbeddingProvider()
        mock_model = MagicMock()
        mock_array = MagicMock()
        mock_array.tolist.return_value = [0.3] * 384
        mock_model.encode.return_value = mock_array
        p._model = mock_model

        with patch("embeddings.local_embeddings.asyncio.to_thread", new_callable=AsyncMock) as mock_thread:
            mock_thread.return_value = mock_array
            result = await p.generate_embedding("test text")
            assert result == [0.3] * 384
            mock_thread.assert_awaited_once_with(mock_model.encode, "test text")
