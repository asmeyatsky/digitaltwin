// Shared configuration for Digital Twin API load tests.
//
// Override BASE_URL at runtime:
//   k6 run -e BASE_URL=https://staging.example.com conversation-flow.js

export const BASE_URL = __ENV.BASE_URL || "http://localhost:8080";

// Test user credentials.
// In development mode the API accepts a single "dev" account whose password
// is read from the DEV_TEST_PASSWORD environment variable on the server side.
// For load tests we pass the password via a k6 environment variable so that
// it never appears in source control.
export const TEST_USER = {
  username: __ENV.TEST_USERNAME || "dev",
  password: __ENV.TEST_PASSWORD || "dev-password",
};

// ── Thresholds ──────────────────────────────────────────────────────────────
// Reusable threshold definitions that individual test scripts can spread into
// their own `options.thresholds` block.

export const COMMON_THRESHOLDS = {
  http_req_duration: ["p(95)<500"],   // 95th percentile response time < 500 ms
  http_req_failed: ["rate<0.01"],     // error rate < 1 %
};

// ── HTTP request defaults ───────────────────────────────────────────────────

export const JSON_HEADERS = {
  "Content-Type": "application/json",
  Accept: "application/json",
};

export function authHeaders(token) {
  return {
    ...JSON_HEADERS,
    Authorization: `Bearer ${token}`,
  };
}
