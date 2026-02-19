// Load test: emotion detection via the conversation message endpoint.
//
// Sends a variety of emotionally charged messages through
// POST /api/conversation/message and verifies that the response includes
// a detected emotion field.
//
// Usage:
//   k6 run emotion-fusion.js
//   k6 run -e BASE_URL=https://staging.example.com emotion-fusion.js

import http from "k6/http";
import { check, group, sleep } from "k6";
import { Counter, Rate, Trend } from "k6/metrics";
import { BASE_URL, COMMON_THRESHOLDS, authHeaders } from "./config.js";
import { authenticate } from "./auth-helper.js";

// ── Custom metrics ──────────────────────────────────────────────────────────

const emotionDetectionDuration = new Trend("emotion_detection_duration", true);
const emotionDetected          = new Rate("emotion_detected_rate");
const emotionMismatch          = new Counter("emotion_mismatch_count");

// ── Options ─────────────────────────────────────────────────────────────────

export const options = {
  scenarios: {
    emotion_throughput: {
      executor: "ramping-vus",
      startVUs: 0,
      stages: [
        { duration: "20s", target: 20 },   // ramp up
        { duration: "2m",  target: 20 },   // sustained load
        { duration: "20s", target: 0 },    // ramp down
      ],
      gracefulRampDown: "10s",
    },
  },
  thresholds: {
    ...COMMON_THRESHOLDS,
    emotion_detection_duration: ["p(95)<700"],
    emotion_detected_rate:      ["rate>0.90"],
  },
};

// ── Emotional message corpus ────────────────────────────────────────────────
// Each entry pairs a message with the emotion keyword the API's fallback
// detector should match (happy, sad, angry, anxious, neutral).

const EMOTION_MESSAGES = [
  { message: "I am so happy today, everything is going wonderfully!",                 expected: "happy"   },
  { message: "I feel excited about the new project we are starting.",                  expected: "happy"   },
  { message: "I have been feeling really sad and down lately.",                        expected: "sad"     },
  { message: "I am depressed and cannot seem to shake this feeling.",                  expected: "sad"     },
  { message: "I am so angry about the way I was treated yesterday.",                   expected: "angry"   },
  { message: "This situation is incredibly frustrating and I have had enough.",         expected: "angry"   },
  { message: "I am worried about what the future holds for me.",                       expected: "anxious" },
  { message: "I feel anxious about my presentation tomorrow.",                         expected: "anxious" },
  { message: "The weather is nice today and I went for a walk.",                       expected: "neutral" },
  { message: "I had lunch and then read a book for a while.",                          expected: "neutral" },
  { message: "Today has been a mixture of good and challenging moments.",              expected: "neutral" },
  { message: "I am extremely happy and grateful for my family.",                       expected: "happy"   },
  { message: "I feel angry and frustrated with my coworkers right now.",               expected: "angry"   },
  { message: "I have been anxious all week about the exam results.",                   expected: "anxious" },
  { message: "I am feeling really sad about losing my pet.",                           expected: "sad"     },
];

// ── Default function ────────────────────────────────────────────────────────

export default function () {
  let token = "";
  let sessionId = "";

  // ── Authenticate ────────────────────────────────────────────────────────
  group("auth", function () {
    token = authenticate();
  });

  if (!token) return;

  // ── Start a conversation session for this VU iteration ──────────────────
  group("start_session", function () {
    const payload = JSON.stringify({
      message: "Starting emotion analysis session.",
    });

    const res = http.post(`${BASE_URL}/api/conversation/start`, payload, {
      headers: authHeaders(token),
      tags: { operation: "emotion_session_start" },
    });

    const ok = check(res, {
      "session start 200": (r) => r.status === 200,
    });

    if (ok) {
      try {
        const body = res.json();
        const data = body.data || body;
        sessionId = data.sessionId;
      } catch (_) {
        // fall through
      }
    }
  });

  if (!sessionId) return;

  sleep(0.5);

  // ── Send emotional messages ─────────────────────────────────────────────
  group("emotion_detection", function () {
    // Pick a random subset of 5 messages per iteration to vary the load.
    const shuffled = EMOTION_MESSAGES.slice().sort(() => Math.random() - 0.5);
    const batch = shuffled.slice(0, 5);

    for (const entry of batch) {
      const payload = JSON.stringify({
        conversationId: sessionId,
        message: entry.message,
      });

      const res = http.post(`${BASE_URL}/api/conversation/message`, payload, {
        headers: authHeaders(token),
        tags: { operation: "emotion_message", expected_emotion: entry.expected },
      });

      emotionDetectionDuration.add(res.timings.duration);

      const hasEmotion = check(res, {
        "status is 200": (r) => r.status === 200,
        "response contains detectedEmotion": (r) => {
          try {
            const body = r.json();
            const data = body.data || body;
            return (
              typeof data.detectedEmotion === "string" &&
              data.detectedEmotion.length > 0
            );
          } catch (_) {
            return false;
          }
        },
      });

      if (hasEmotion) {
        emotionDetected.add(1);

        // Optional soft check: does the detected emotion match the expected one?
        try {
          const body = res.json();
          const data = body.data || body;
          const detected = data.detectedEmotion.toLowerCase();
          if (detected !== entry.expected) {
            emotionMismatch.add(1);
          }
        } catch (_) {
          // ignore parse errors here
        }
      } else {
        emotionDetected.add(0);
      }

      // Brief pause between messages.
      sleep(Math.random() * 0.5 + 0.3);
    }
  });

  // ── Cleanup: end the session ────────────────────────────────────────────
  group("end_session", function () {
    const payload = JSON.stringify({
      conversationId: sessionId,
    });

    http.post(`${BASE_URL}/api/conversation/end`, payload, {
      headers: authHeaders(token),
      tags: { operation: "emotion_session_end" },
    });
  });

  sleep(0.5);
}
