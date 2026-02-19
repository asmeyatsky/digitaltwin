#!/usr/bin/env bash
# run-all.sh -- Run all Digital Twin k6 load tests sequentially and print a
# summary at the end.
#
# Usage:
#   ./run-all.sh
#   BASE_URL=https://staging.example.com ./run-all.sh
#   TEST_USERNAME=dev TEST_PASSWORD=secret ./run-all.sh

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

# Propagate environment variables to k6 via -e flags.
K6_ENV_FLAGS=""
[ -n "${BASE_URL:-}" ]      && K6_ENV_FLAGS="$K6_ENV_FLAGS -e BASE_URL=$BASE_URL"
[ -n "${TEST_USERNAME:-}" ] && K6_ENV_FLAGS="$K6_ENV_FLAGS -e TEST_USERNAME=$TEST_USERNAME"
[ -n "${TEST_PASSWORD:-}" ] && K6_ENV_FLAGS="$K6_ENV_FLAGS -e TEST_PASSWORD=$TEST_PASSWORD"

# Ensure k6 is installed.
if ! command -v k6 &>/dev/null; then
  echo "ERROR: k6 is not installed. Install it from https://k6.io/docs/getting-started/installation/"
  exit 1
fi

SEPARATOR="============================================================"
TESTS=(
  "conversation-flow.js:Conversation Lifecycle"
  "emotion-fusion.js:Emotion Detection Throughput"
  "websocket-connections.js:WebSocket Connections"
)

PASS_COUNT=0
FAIL_COUNT=0
RESULTS=()

echo ""
echo "$SEPARATOR"
echo "  Digital Twin API -- Load Test Suite"
echo "  $(date '+%Y-%m-%d %H:%M:%S %Z')"
echo "$SEPARATOR"
echo ""

for entry in "${TESTS[@]}"; do
  FILE="${entry%%:*}"
  LABEL="${entry##*:}"

  echo "$SEPARATOR"
  echo "  Running: $LABEL ($FILE)"
  echo "$SEPARATOR"
  echo ""

  if k6 run $K6_ENV_FLAGS "$SCRIPT_DIR/$FILE"; then
    RESULTS+=("PASS  $LABEL")
    PASS_COUNT=$((PASS_COUNT + 1))
  else
    RESULTS+=("FAIL  $LABEL")
    FAIL_COUNT=$((FAIL_COUNT + 1))
  fi

  echo ""
done

# ── Summary ────────────────────────────────────────────────────────────────

echo "$SEPARATOR"
echo "  SUMMARY"
echo "$SEPARATOR"
echo ""

for result in "${RESULTS[@]}"; do
  echo "  $result"
done

echo ""
echo "  Total: $((PASS_COUNT + FAIL_COUNT))  |  Passed: $PASS_COUNT  |  Failed: $FAIL_COUNT"
echo ""

if [ "$FAIL_COUNT" -gt 0 ]; then
  echo "  Some tests FAILED."
  exit 1
else
  echo "  All tests PASSED."
  exit 0
fi
