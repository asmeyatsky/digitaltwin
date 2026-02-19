// Helper module: authenticates against the Digital Twin API and returns a JWT
// token that other test scripts can use for authorized requests.

import http from "k6/http";
import { check } from "k6";
import { BASE_URL, TEST_USER, JSON_HEADERS } from "./config.js";

/**
 * Log in with the configured test user and return the JWT token string.
 * Aborts the VU iteration (via `check` failure) when authentication fails.
 *
 * @param {object} [overrides]             Optional credential overrides.
 * @param {string} [overrides.username]    Username (defaults to TEST_USER.username).
 * @param {string} [overrides.password]    Password (defaults to TEST_USER.password).
 * @returns {string} JWT token.
 */
export function authenticate(overrides = {}) {
  const payload = JSON.stringify({
    username: overrides.username || TEST_USER.username,
    password: overrides.password || TEST_USER.password,
  });

  const res = http.post(`${BASE_URL}/api/auth/login`, payload, {
    headers: JSON_HEADERS,
    tags: { operation: "login" },
  });

  const ok = check(res, {
    "login status is 200": (r) => r.status === 200,
    "login response has token": (r) => {
      try {
        const body = r.json();
        return body.token !== undefined && body.token !== null && body.token !== "";
      } catch (_) {
        return false;
      }
    },
  });

  if (!ok) {
    console.error(
      `Authentication failed (status ${res.status}): ${res.body}`
    );
    return "";
  }

  return res.json().token;
}
