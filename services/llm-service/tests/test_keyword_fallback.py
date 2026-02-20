"""Tests for the keyword-based emotion fallback in main.py."""

from main import _keyword_emotion_fallback


class TestKeywordEmotionFallback:
    def test_happy_keywords(self):
        emotion, conf = _keyword_emotion_fallback("I am so happy and glad today")
        assert emotion == "happy"
        assert conf > 0.4

    def test_sad_keywords(self):
        emotion, _ = _keyword_emotion_fallback("I feel sad and lonely")
        assert emotion == "sad"

    def test_angry_keywords(self):
        emotion, _ = _keyword_emotion_fallback("I am so angry and frustrated")
        assert emotion == "angry"

    def test_anxious_keywords(self):
        emotion, _ = _keyword_emotion_fallback("I feel anxious and worried")
        assert emotion == "anxious"

    def test_calm_keywords(self):
        emotion, _ = _keyword_emotion_fallback("I feel calm and peaceful")
        assert emotion == "calm"

    def test_surprised_keywords(self):
        emotion, _ = _keyword_emotion_fallback("I was so surprised and shocked")
        assert emotion == "surprised"

    def test_excited_keywords(self):
        emotion, _ = _keyword_emotion_fallback("I am so excited and thrilled")
        assert emotion == "excited"

    def test_neutral_default(self):
        emotion, conf = _keyword_emotion_fallback("the weather is moderate")
        assert emotion == "neutral"
        assert conf == 0.5

    def test_confidence_increases_with_matches(self):
        _, conf_one = _keyword_emotion_fallback("happy")
        _, conf_two = _keyword_emotion_fallback("happy and joy")
        assert conf_two > conf_one

    def test_confidence_capped_at_085(self):
        # Use many happy keywords to try to exceed cap
        _, conf = _keyword_emotion_fallback(
            "happy joy wonderful great amazing love fantastic glad"
        )
        assert conf <= 0.85

    def test_case_insensitive(self):
        emotion, _ = _keyword_emotion_fallback("I am HAPPY and GLAD")
        assert emotion == "happy"
