"""Tests for provider factory functions."""

import pytest

from providers import create_llm_provider
from providers.openai_provider import OpenAIProvider
from providers.anthropic_provider import AnthropicProvider
from providers.google_provider import GoogleProvider
from embeddings import create_embedding_provider
from embeddings.openai_embeddings import OpenAIEmbeddingProvider
from embeddings.google_embeddings import GoogleEmbeddingProvider
from embeddings.local_embeddings import LocalEmbeddingProvider


class TestCreateLLMProvider:
    def test_default_is_openai(self, monkeypatch):
        monkeypatch.delenv("LLM_PROVIDER", raising=False)
        monkeypatch.delenv("LLM_MODEL", raising=False)
        p = create_llm_provider()
        assert isinstance(p, OpenAIProvider)

    def test_openai_provider(self, monkeypatch):
        monkeypatch.setenv("LLM_PROVIDER", "openai")
        p = create_llm_provider()
        assert isinstance(p, OpenAIProvider)

    def test_anthropic_provider(self, monkeypatch):
        monkeypatch.setenv("LLM_PROVIDER", "anthropic")
        p = create_llm_provider()
        assert isinstance(p, AnthropicProvider)

    def test_google_provider(self, monkeypatch):
        monkeypatch.setenv("LLM_PROVIDER", "google")
        p = create_llm_provider()
        assert isinstance(p, GoogleProvider)

    def test_unknown_falls_back_to_openai(self, monkeypatch):
        monkeypatch.setenv("LLM_PROVIDER", "unknown-provider")
        p = create_llm_provider()
        assert isinstance(p, OpenAIProvider)

    def test_custom_model(self, monkeypatch):
        monkeypatch.setenv("LLM_PROVIDER", "openai")
        monkeypatch.setenv("LLM_MODEL", "gpt-4-turbo")
        p = create_llm_provider()
        assert p.model == "gpt-4-turbo"


class TestCreateEmbeddingProvider:
    def test_default_is_openai(self, monkeypatch):
        monkeypatch.delenv("EMBEDDING_PROVIDER", raising=False)
        p = create_embedding_provider()
        assert isinstance(p, OpenAIEmbeddingProvider)

    def test_google_provider(self, monkeypatch):
        monkeypatch.setenv("EMBEDDING_PROVIDER", "google")
        p = create_embedding_provider()
        assert isinstance(p, GoogleEmbeddingProvider)

    def test_local_provider(self, monkeypatch):
        monkeypatch.setenv("EMBEDDING_PROVIDER", "local")
        p = create_embedding_provider()
        assert isinstance(p, LocalEmbeddingProvider)

    def test_unknown_falls_back_to_openai(self, monkeypatch):
        monkeypatch.setenv("EMBEDDING_PROVIDER", "unknown")
        p = create_embedding_provider()
        assert isinstance(p, OpenAIEmbeddingProvider)
