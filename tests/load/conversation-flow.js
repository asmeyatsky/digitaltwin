// Load test: full conversation lifecycle
//
//   Login -> Health check -> Start conversation -> Send 5 messages -> End conversation
//
// Usage:
//   k6 run conversation-flow.js
//   k6 run -e BASE_URL=https://staging.example.com conversation-flow.js

import http from "k6/http";
import { check, group, sleep } from "k6";
import { Counter, Rate, Trend } from "k6/metrics";
import { BASE_URL, COMMON_THRESHOLDS, authHeaders } from "./config.js";
import { authenticate } from "./auth-helper.js";

// ── Custom metrics ──────────────────────────────────────────────────────────

const conversationStartDuration = new Trend("conversation_start_duration", true);
const messageSendDuration       = new Trend("message_send_duration", true);
const conversationEndDuration   = new Trend("conversation_end_duration", true);
const messageErrors             = new Counter("message_errors");
const messageSuccessRate        = new Rate("message_success_rate");

// ── Options ─────────────────────────────────────────────────────────────────

export const options = {
  scenarios: {
    conversation_lifecycle: {
      executor: "ramping-vus",
      startVUs: 0,
      stages: [
        { duration: "30s", target: 10 },   // ramp up
        { duration: "2m",  target: 10 },   // hold
        { duration: "30s", target: 0 },    // ramp down
      ],
      gracefulRampDown: "10s",
    },
  },
  thresholds: {
    ...COMMON_THRESHOLDS,
    conversation_start_duration: ["p(95)<600"],
    message_send_duration:       ["p(95)<800"],
    conversation_end_duration:   ["p(95)<400"],
    message_success_rate:        ["rate>0.95"],
  },
};

// ── Test messages ───────────────────────────────────────────────────────────

const MESSAGES = [
  "I have been feeling really happy today, everything seems to be going well.",
  "Work has been stressful lately and I am a bit overwhelmed.",
  "I just had an amazing conversation with a friend and feel grateful.",
  "I am worried about my upcoming deadline, any advice?",
  "Thanks for listening, I feel much better now.",
];

// ── Default function (VU entrypoint) ────────────────────────────────────────

export default function () {
  let token = "";
  let sessionId = "";

  // ── Phase 1: Authenticate ───────────────────────────────────────────────
  group("01_authenticate", function () {
    token = authenticate();
    if (!token) {
      console.error("Skipping iteration -- authentication failed");
      return;
    }
  });

  if (!token) return;

  // ── Phase 2: Health check ───────────────────────────────────────────────
  group("02_health_check", function () {
    const res = http.get(`${BASE_URL}/health`, {
      tags: { operation: "health_check" },
    });
    check(res, {
      "health status is 200": (r) => r.status === 200,
    });
  });

  sleep(0.5);

  // ── Phase 3: Start conversation ─────────────────────────────────────────
  group("03_start_conversation", function () {
    const payload = JSON.stringify({
      message: "Hello, I would like to start a conversation.",
    });

    const res = http.post(`${BASE_URL}/api/conversation/start`, payload, {
      headers: authHeaders(token),
      tags: { operation: "conversation_start" },
    });

    conversationStartDuration.add(res.timings.duration);

    const ok = check(res, {
      "start status is 200":     (r) => r.status === 200,
      "start returns sessionId": (r) => {
        try {
          const body = r.json();
          const data = body.data || body;
          return data.sessionId !== undefined;
        } catch (_) {
          return false;
        }
      },
      "start returns response": (r) => {
        try {
          const body = r.json();
          const data = body.data || body;
          return typeof data.response === "string" && data.response.length > 0;
        } catch (_) {
          return false;
        }
      },
    });

    if (ok) {
      try {
        const body = res.json();
        const data = body.data || body;
        sessionId = data.sessionId;
      } catch (_) {
        // handled by check failure
      }
    }
  });

  if (!sessionId) return;

  sleep(1);

  // ── Phase 4: Send messages ──────────────────────────────────────────────
  group("04_send_messages", function () {
    for (let i = 0; i < MESSAGES.length; i++) {
      const payload = JSON.stringify({
        conversationId: sessionId,
        message: MESSAGES[i],
      });

      const res = http.post(`${BASE_URL}/api/conversation/message`, payload, {
        headers: authHeaders(token),
        tags: { operation: "send_message", message_index: String(i) },
      });

      messageSendDuration.add(res.timings.duration);

      const ok = check(res, {
        "message status is 200": (r) => r.status === 200,
        "message has response":  (r) => {
          try {
            const body = r.json();
            const data = body.data || body;
            return typeof data.response === "string" && data.response.length > 0;
          } catch (_) {
            return false;
          }
        },
        "message has detectedEmotion": (r) => {
          try {
            const body = r.json();
            const data = body.data || body;
            return typeof data.detectedEmotion === "string";
          } catch (_) {
            return false;
          }
        },
      });

      if (ok) {
        messageSuccessRate.add(1);
      } else {
        messageSuccessRate.add(0);
        messageErrors.add(1);
      }

      // Simulate human-like thinking pause between messages.
      sleep(Math.random() * 2 + 1); // 1-3 seconds
    }
  });

  // ── Phase 5: End conversation ───────────────────────────────────────────
  group("05_end_conversation", function () {
    const payload = JSON.stringify({
      conversationId: sessionId,
      sessionDuration: "00:05:00", // 5 minutes simulated duration
    });

    const res = http.post(`${BASE_URL}/api/conversation/end`, payload, {
      headers: authHeaders(token),
      tags: { operation: "conversation_end" },
    });

    conversationEndDuration.add(res.timings.duration);

    check(res, {
      "end status is 200":      (r) => r.status === 200,
      "end confirms success":   (r) => {
        try {
          const body = r.json();
          return body.success === true || body.message !== undefined;
        } catch (_) {
          return false;
        }
      },
    });
  });

  sleep(1);
}
