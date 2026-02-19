"""Google (Gemini) LLM provider implementation."""

import json
import logging
import os
import re

import google.generativeai as genai

from .base import LLMProvider

logger = logging.getLogger(__name__)

DEFAULT_MODEL = "gemini-2.0-flash"
MAX_TOKENS = 150
TEMPERATURE = 0.7


class GoogleProvider(LLMProvider):
    provider_name = "google"

    def __init__(self, model: str | None = None):
        self.model = model or DEFAULT_MODEL
        self._configured = False

    def is_available(self) -> bool:
        return bool(os.getenv("GOOGLE_API_KEY"))

    def _ensure_configured(self):
        if not self._configured:
            genai.configure(api_key=os.getenv("GOOGLE_API_KEY"))
            self._configured = True

    @staticmethod
    def _strip_code_fences(text: str) -> str:
        """Strip markdown code fences that Gemini sometimes wraps around JSON."""
        text = text.strip()
        text = re.sub(r"^```(?:json)?\s*\n?", "", text)
        text = re.sub(r"\n?```\s*$", "", text)
        return text.strip()

    async def generate_chat_response(
        self, system_prompt: str, user_message: str, context: str = ""
    ) -> str:
        self._ensure_configured()

        system = system_prompt
        if context:
            system += f"\n\nRecent conversation context:\n{context}"

        model = genai.GenerativeModel(
            model_name=self.model,
            system_instruction=system,
            generation_config=genai.GenerationConfig(
                max_output_tokens=MAX_TOKENS,
                temperature=TEMPERATURE,
            ),
        )

        response = await model.generate_content_async(user_message)
        return response.text.strip()

    async def classify_emotion(self, text: str, emotions: list[str]) -> dict:
        self._ensure_configured()
        emotion_list = ", ".join(emotions)

        model = genai.GenerativeModel(
            model_name=self.model,
            system_instruction=(
                f"You are an emotion classifier. Classify the user's text into exactly one of these emotions: "
                f"{emotion_list}. "
                "Also provide a confidence score between 0 and 1. "
                'Respond ONLY with JSON: {"emotion": "...", "confidence": 0.X}'
            ),
            generation_config=genai.GenerationConfig(
                max_output_tokens=50,
                temperature=0.1,
            ),
        )

        response = await model.generate_content_async(text)
        result_text = self._strip_code_fences(response.text)
        return json.loads(result_text)
