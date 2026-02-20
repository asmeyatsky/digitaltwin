"""Tests for the Anthropic LLM provider."""

import json
from unittest.mock import AsyncMock, MagicMock

import pytest

from providers.anthropic_provider import AnthropicProvider, DEFAULT_MODEL


class TestAnthropicProvider:
    def test_provider_name(self):
        p = AnthropicProvider()
        assert p.provider_name == "anthropic"

    def test_default_model(self):
        p = AnthropicProvider()
        assert p.model == DEFAULT_MODEL

    def test_custom_model(self):
        p = AnthropicProvider(model="claude-3-haiku-20240307")
        assert p.model == "claude-3-haiku-20240307"

    def test_is_available_with_key(self, monkeypatch):
        monkeypatch.setenv("ANTHROPIC_API_KEY", "sk-ant-test")
        p = AnthropicProvider()
        assert p.is_available() is True

    def test_is_available_without_key(self, monkeypatch):
        monkeypatch.delenv("ANTHROPIC_API_KEY", raising=False)
        p = AnthropicProvider()
        assert p.is_available() is False

    @pytest.mark.asyncio
    async def test_generate_uses_system_kwarg(self):
        """Anthropic uses system= kwarg, NOT system messages in the list."""
        p = AnthropicProvider()
        mock_client = AsyncMock()
        mock_block = MagicMock()
        mock_block.text = "response text"
        mock_client.messages.create.return_value = MagicMock(content=[mock_block])
        p._client = mock_client

        await p.generate_chat_response("Be helpful", "hello", context="ctx")
        call_kwargs = mock_client.messages.create.call_args.kwargs
        # system should be in kwargs, not in messages
        assert "system" in call_kwargs
        assert "ctx" in call_kwargs["system"]
        assert call_kwargs["messages"] == [{"role": "user", "content": "hello"}]

    @pytest.mark.asyncio
    async def test_classify_with_code_fences(self):
        p = AnthropicProvider()
        mock_client = AsyncMock()
        mock_block = MagicMock()
        mock_block.text = '```json\n{"emotion": "sad", "confidence": 0.8}\n```'
        mock_client.messages.create.return_value = MagicMock(content=[mock_block])
        p._client = mock_client

        result = await p.classify_emotion("I am sad", ["happy", "sad"])
        assert result["emotion"] == "sad"

    @pytest.mark.asyncio
    async def test_classify_without_code_fences(self):
        p = AnthropicProvider()
        mock_client = AsyncMock()
        mock_block = MagicMock()
        mock_block.text = '{"emotion": "happy", "confidence": 0.85}'
        mock_client.messages.create.return_value = MagicMock(content=[mock_block])
        p._client = mock_client

        result = await p.classify_emotion("great day", ["happy", "sad"])
        assert result["emotion"] == "happy"

    def test_strip_code_fences_json(self):
        assert AnthropicProvider._strip_code_fences('```json\n{"a":1}\n```') == '{"a":1}'

    def test_strip_code_fences_plain(self):
        assert AnthropicProvider._strip_code_fences('```\n{"a":1}\n```') == '{"a":1}'

    def test_strip_code_fences_no_fences(self):
        assert AnthropicProvider._strip_code_fences('{"a":1}') == '{"a":1}'
