"""Tests for the Google (Gemini) LLM provider."""

from unittest.mock import AsyncMock, MagicMock, patch

import pytest

from providers.google_provider import GoogleProvider, DEFAULT_MODEL


class TestGoogleProvider:
    def test_provider_name(self):
        p = GoogleProvider()
        assert p.provider_name == "google"

    def test_default_model(self):
        p = GoogleProvider()
        assert p.model == DEFAULT_MODEL

    def test_is_available_with_key(self, monkeypatch):
        monkeypatch.setenv("GOOGLE_API_KEY", "goog-test")
        p = GoogleProvider()
        assert p.is_available() is True

    def test_is_available_without_key(self, monkeypatch):
        monkeypatch.delenv("GOOGLE_API_KEY", raising=False)
        p = GoogleProvider()
        assert p.is_available() is False

    @pytest.mark.asyncio
    async def test_generate_uses_system_instruction(self, monkeypatch):
        """Google provider uses system_instruction on the GenerativeModel."""
        monkeypatch.setenv("GOOGLE_API_KEY", "test-key")
        p = GoogleProvider()

        mock_model_instance = MagicMock()
        mock_model_instance.generate_content_async = AsyncMock(
            return_value=MagicMock(text="  gemini response  ")
        )

        with patch("providers.google_provider.genai") as mock_genai:
            mock_genai.GenerativeModel.return_value = mock_model_instance
            mock_genai.GenerationConfig = MagicMock()

            result = await p.generate_chat_response("Be helpful", "hello")
            assert result == "gemini response"

            # Verify system_instruction was passed
            call_kwargs = mock_genai.GenerativeModel.call_args.kwargs
            assert "system_instruction" in call_kwargs

    @pytest.mark.asyncio
    async def test_classify_strips_code_fences(self, monkeypatch):
        monkeypatch.setenv("GOOGLE_API_KEY", "test-key")
        p = GoogleProvider()

        mock_model_instance = MagicMock()
        mock_model_instance.generate_content_async = AsyncMock(
            return_value=MagicMock(text='```json\n{"emotion":"calm","confidence":0.7}\n```')
        )

        with patch("providers.google_provider.genai") as mock_genai:
            mock_genai.GenerativeModel.return_value = mock_model_instance
            mock_genai.GenerationConfig = MagicMock()

            result = await p.classify_emotion("peaceful day", ["calm", "happy"])
            assert result["emotion"] == "calm"

    @pytest.mark.asyncio
    async def test_ensure_configured_called_once(self, monkeypatch):
        monkeypatch.setenv("GOOGLE_API_KEY", "test-key")
        p = GoogleProvider()

        mock_model_instance = MagicMock()
        mock_model_instance.generate_content_async = AsyncMock(
            return_value=MagicMock(text="ok")
        )

        with patch("providers.google_provider.genai") as mock_genai:
            mock_genai.GenerativeModel.return_value = mock_model_instance
            mock_genai.GenerationConfig = MagicMock()

            await p.generate_chat_response("sys", "msg1")
            await p.generate_chat_response("sys", "msg2")

            # configure should only be called once
            assert mock_genai.configure.call_count == 1
