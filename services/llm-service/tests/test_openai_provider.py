"""Tests for the OpenAI LLM provider."""

import json
from unittest.mock import AsyncMock, MagicMock, patch

import pytest

from providers.openai_provider import OpenAIProvider, DEFAULT_MODEL


class TestOpenAIProvider:
    def test_provider_name(self):
        p = OpenAIProvider()
        assert p.provider_name == "openai"

    def test_default_model(self):
        p = OpenAIProvider()
        assert p.model == DEFAULT_MODEL

    def test_custom_model(self):
        p = OpenAIProvider(model="gpt-4")
        assert p.model == "gpt-4"

    def test_is_available_with_key(self, monkeypatch):
        monkeypatch.setenv("OPENAI_API_KEY", "sk-test")
        p = OpenAIProvider()
        assert p.is_available() is True

    def test_is_available_without_key(self, monkeypatch):
        monkeypatch.delenv("OPENAI_API_KEY", raising=False)
        p = OpenAIProvider()
        assert p.is_available() is False

    @pytest.mark.asyncio
    async def test_generate_chat_response_without_context(self):
        p = OpenAIProvider()
        mock_client = AsyncMock()
        mock_choice = MagicMock()
        mock_choice.message.content = "  Hello there  "
        mock_client.chat.completions.create.return_value = MagicMock(
            choices=[mock_choice]
        )
        p._client = mock_client

        result = await p.generate_chat_response("system", "hello")
        assert result == "Hello there"

        call_kwargs = mock_client.chat.completions.create.call_args
        messages = call_kwargs.kwargs["messages"]
        # Should have system + user, no context
        assert len(messages) == 2
        assert messages[0]["role"] == "system"
        assert messages[1]["role"] == "user"

    @pytest.mark.asyncio
    async def test_generate_chat_response_with_context(self):
        p = OpenAIProvider()
        mock_client = AsyncMock()
        mock_choice = MagicMock()
        mock_choice.message.content = "response"
        mock_client.chat.completions.create.return_value = MagicMock(
            choices=[mock_choice]
        )
        p._client = mock_client

        await p.generate_chat_response("system", "hello", context="prev convo")
        call_kwargs = mock_client.chat.completions.create.call_args
        messages = call_kwargs.kwargs["messages"]
        # system + context system + user
        assert len(messages) == 3
        assert "context" in messages[1]["content"].lower()

    @pytest.mark.asyncio
    async def test_classify_emotion(self):
        p = OpenAIProvider()
        mock_client = AsyncMock()
        mock_choice = MagicMock()
        mock_choice.message.content = '{"emotion": "happy", "confidence": 0.9}'
        mock_client.chat.completions.create.return_value = MagicMock(
            choices=[mock_choice]
        )
        p._client = mock_client

        result = await p.classify_emotion("I feel great", ["happy", "sad"])
        assert result["emotion"] == "happy"
        assert result["confidence"] == 0.9

    @pytest.mark.asyncio
    async def test_classify_emotion_json_parse_error(self):
        p = OpenAIProvider()
        mock_client = AsyncMock()
        mock_choice = MagicMock()
        mock_choice.message.content = "not valid json"
        mock_client.chat.completions.create.return_value = MagicMock(
            choices=[mock_choice]
        )
        p._client = mock_client

        with pytest.raises(json.JSONDecodeError):
            await p.classify_emotion("text", ["happy"])
