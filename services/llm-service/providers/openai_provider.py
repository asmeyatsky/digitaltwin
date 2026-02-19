"""OpenAI LLM provider implementation."""

import json
import logging
import os

import openai

from .base import LLMProvider

logger = logging.getLogger(__name__)

DEFAULT_MODEL = "gpt-4o-mini"
MAX_TOKENS = 150
TEMPERATURE = 0.7


class OpenAIProvider(LLMProvider):
    provider_name = "openai"

    def __init__(self, model: str | None = None):
        self.model = model or DEFAULT_MODEL
        self._client: openai.AsyncOpenAI | None = None

    def is_available(self) -> bool:
        return bool(os.getenv("OPENAI_API_KEY"))

    def _get_client(self) -> openai.AsyncOpenAI:
        if self._client is None:
            self._client = openai.AsyncOpenAI()
        return self._client

    async def generate_chat_response(
        self, system_prompt: str, user_message: str, context: str = ""
    ) -> str:
        client = self._get_client()

        messages = [{"role": "system", "content": system_prompt}]
        if context:
            messages.append(
                {"role": "system", "content": f"Recent conversation context:\n{context}"}
            )
        messages.append({"role": "user", "content": user_message})

        response = await client.chat.completions.create(
            model=self.model,
            messages=messages,
            max_tokens=MAX_TOKENS,
            temperature=TEMPERATURE,
            stream=False,
        )

        return response.choices[0].message.content.strip()

    async def classify_emotion(self, text: str, emotions: list[str]) -> dict:
        client = self._get_client()
        emotion_list = ", ".join(emotions)

        response = await client.chat.completions.create(
            model=self.model,
            messages=[
                {
                    "role": "system",
                    "content": (
                        f"You are an emotion classifier. Classify the user's text into exactly one of these emotions: "
                        f"{emotion_list}. "
                        "Also provide a confidence score between 0 and 1. "
                        'Respond ONLY with JSON: {"emotion": "...", "confidence": 0.X}'
                    ),
                },
                {"role": "user", "content": text},
            ],
            max_tokens=50,
            temperature=0.1,
        )

        result_text = response.choices[0].message.content.strip()
        return json.loads(result_text)
