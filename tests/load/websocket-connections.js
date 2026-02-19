// Load test: SignalR WebSocket connections to /hubs/companion.
//
// Each VU authenticates, opens a WebSocket connection to the CompanionHub,
// joins a room, sends messages and emotions, then disconnects.
//
// k6 WebSocket support uses the built-in `k6/ws` module.  SignalR over
// WebSocket uses a specific handshake protocol (JSON with record separator
// 0x1E). This script implements the minimal SignalR handshake so that the
// server accepts the connection.
//
// Usage:
//   k6 run websocket-connections.js
//   k6 run -e BASE_URL=http://localhost:8080 websocket-connections.js

import ws from "k6/ws";
import { check, group, sleep } from "k6";
import { Counter, Rate, Trend } from "k6/metrics";
import { BASE_URL, COMMON_THRESHOLDS } from "./config.js";
import { authenticate } from "./auth-helper.js";

// ── Custom metrics ──────────────────────────────────────────────────────────

const wsConnectDuration  = new Trend("ws_connect_duration", true);
const wsMessagesSent     = new Counter("ws_messages_sent");
const wsMessagesReceived = new Counter("ws_messages_received");
const wsConnectionRate   = new Rate("ws_connection_success_rate");
const wsErrors           = new Counter("ws_errors");

// ── Options ─────────────────────────────────────────────────────────────────

export const options = {
  scenarios: {
    websocket_load: {
      executor: "ramping-vus",
      startVUs: 0,
      stages: [
        { duration: "30s", target: 25 },   // ramp to half
        { duration: "30s", target: 50 },   // ramp to full
        { duration: "2m",  target: 50 },   // sustained
        { duration: "30s", target: 0 },    // ramp down
      ],
      gracefulRampDown: "15s",
    },
  },
  thresholds: {
    ...COMMON_THRESHOLDS,
    ws_connect_duration:       ["p(95)<2000"],
    ws_connection_success_rate: ["rate>0.90"],
  },
};

// ── SignalR protocol helpers ────────────────────────────────────────────────

const RECORD_SEPARATOR = "\u001e";

function signalrMessage(type, target, args) {
  return JSON.stringify({ type, target, arguments: args }) + RECORD_SEPARATOR;
}

// SignalR handshake request (JSON protocol, version 1).
const HANDSHAKE_REQUEST =
  JSON.stringify({ protocol: "json", version: 1 }) + RECORD_SEPARATOR;

// ── Room & message data ─────────────────────────────────────────────────────

// Shared room IDs that VUs will join. Using a small pool creates realistic
// multi-user rooms.
const ROOM_IDS = [
  "00000000-0000-0000-0000-000000000001",
  "00000000-0000-0000-0000-000000000002",
  "00000000-0000-0000-0000-000000000003",
];

const CHAT_MESSAGES = [
  "Hello everyone!",
  "How is everyone feeling today?",
  "I have been thinking about mindfulness lately.",
  "Let us do a quick breathing exercise together.",
  "Thank you all for being here.",
];

const EMOTIONS = [
  { emotion: "happy",   confidence: 0.92 },
  { emotion: "calm",    confidence: 0.85 },
  { emotion: "anxious", confidence: 0.78 },
  { emotion: "sad",     confidence: 0.70 },
  { emotion: "neutral", confidence: 0.60 },
];

// ── Default function ────────────────────────────────────────────────────────

export default function () {
  // ── Step 1: Obtain JWT token via REST ───────────────────────────────────
  let token = "";
  group("ws_authenticate", function () {
    token = authenticate();
  });

  if (!token) {
    wsConnectionRate.add(0);
    return;
  }

  // ── Step 2: Build WebSocket URL ─────────────────────────────────────────
  // Replace http(s) with ws(s) and append the JWT as a query parameter
  // (SignalR's default browser transport sends the token this way).
  const wsBase = BASE_URL.replace(/^http/, "ws");
  const url = `${wsBase}/hubs/companion?access_token=${encodeURIComponent(token)}`;

  const roomId = ROOM_IDS[Math.floor(Math.random() * ROOM_IDS.length)];

  // ── Step 3: Open WebSocket connection ───────────────────────────────────
  const startTime = Date.now();

  const res = ws.connect(url, {}, function (socket) {
    let handshakeComplete = false;

    socket.on("open", function () {
      const connectTime = Date.now() - startTime;
      wsConnectDuration.add(connectTime);
      wsConnectionRate.add(1);

      // Send SignalR handshake.
      socket.send(HANDSHAKE_REQUEST);
    });

    socket.on("message", function (data) {
      wsMessagesReceived.add(1);

      // ── Handle handshake response ─────────────────────────────────────
      if (!handshakeComplete) {
        // A successful handshake response is `{}\x1e`.
        if (data.indexOf("{}") !== -1) {
          handshakeComplete = true;

          // Join room (SignalR invocation type = 1).
          socket.send(
            signalrMessage(1, "JoinRoom", [roomId])
          );
          wsMessagesSent.add(1);
        } else {
          wsErrors.add(1);
          socket.close();
          return;
        }
      }

      // Parse incoming SignalR messages for logging / verification.
      try {
        const parts = data.split(RECORD_SEPARATOR).filter(Boolean);
        for (const part of parts) {
          const msg = JSON.parse(part);
          // Type 1 = invocation (server calling client method).
          if (msg.type === 1 && msg.target) {
            // We successfully received a hub invocation.
          }
        }
      } catch (_) {
        // Not all frames are JSON (ping frames, etc.) -- ignore.
      }
    });

    socket.on("error", function (e) {
      wsErrors.add(1);
      console.error(`WebSocket error: ${e.error()}`);
    });

    socket.on("close", function () {
      // Connection closed.
    });

    // ── After handshake, perform actions inside the room ────────────────
    // Give the handshake a moment to complete.
    socket.setTimeout(function () {
      if (!handshakeComplete) {
        wsErrors.add(1);
        socket.close();
        return;
      }

      // Send chat messages.
      for (let i = 0; i < CHAT_MESSAGES.length; i++) {
        socket.send(
          signalrMessage(1, "SendMessage", [roomId, CHAT_MESSAGES[i]])
        );
        wsMessagesSent.add(1);
      }
    }, 2000);

    // Share emotions after a brief delay.
    socket.setTimeout(function () {
      if (!handshakeComplete) return;

      const emo = EMOTIONS[Math.floor(Math.random() * EMOTIONS.length)];
      socket.send(
        signalrMessage(1, "ShareEmotion", [roomId, emo.emotion, emo.confidence])
      );
      wsMessagesSent.add(1);
    }, 5000);

    // Leave room and close after the session.
    socket.setTimeout(function () {
      if (handshakeComplete) {
        socket.send(
          signalrMessage(1, "LeaveRoom", [roomId])
        );
        wsMessagesSent.add(1);
      }
      socket.close();
    }, 10000);
  });

  check(res, {
    "WebSocket status is 101": (r) => r && r.status === 101,
  });

  if (!res || res.status !== 101) {
    wsConnectionRate.add(0);
  }

  sleep(1);
}
