"""Anthropic (Claude) LLM provider implementation."""

import json
import logging
import os
import re

import anthropic

from .base import LLMProvider

logger = logging.getLogger(__name__)

DEFAULT_MODEL = "claude-sonnet-4-20250514"
MAX_TOKENS = 150
TEMPERATURE = 0.7


class AnthropicProvider(LLMProvider):
    provider_name = "anthropic"

    def __init__(self, model: str | None = None):
        self.model = model or DEFAULT_MODEL
        self._client: anthropic.AsyncAnthropic | None = None

    def is_available(self) -> bool:
        return bool(os.getenv("ANTHROPIC_API_KEY"))

    def _get_client(self) -> anthropic.AsyncAnthropic:
        if self._client is None:
            self._client = anthropic.AsyncAnthropic()
        return self._client

    @staticmethod
    def _strip_code_fences(text: str) -> str:
        """Strip markdown code fences that Claude sometimes wraps around JSON."""
        text = text.strip()
        text = re.sub(r"^```(?:json)?\s*\n?", "", text)
        text = re.sub(r"\n?```\s*$", "", text)
        return text.strip()

    async def generate_chat_response(
        self, system_prompt: str, user_message: str, context: str = ""
    ) -> str:
        client = self._get_client()

        system = system_prompt
        if context:
            system += f"\n\nRecent conversation context:\n{context}"

        response = await client.messages.create(
            model=self.model,
            max_tokens=MAX_TOKENS,
            temperature=TEMPERATURE,
            system=system,
            messages=[{"role": "user", "content": user_message}],
        )

        return response.content[0].text.strip()

    async def classify_emotion(self, text: str, emotions: list[str]) -> dict:
        client = self._get_client()
        emotion_list = ", ".join(emotions)

        response = await client.messages.create(
            model=self.model,
            max_tokens=50,
            temperature=0.1,
            system=(
                f"You are an emotion classifier. Classify the user's text into exactly one of these emotions: "
                f"{emotion_list}. "
                "Also provide a confidence score between 0 and 1. "
                'Respond ONLY with JSON: {"emotion": "...", "confidence": 0.X}'
            ),
            messages=[{"role": "user", "content": text}],
        )

        result_text = self._strip_code_fences(response.content[0].text)
        return json.loads(result_text)
