# DELTA.md — Remaining Implementation Plan

**Date:** 2026-02-19
**Baseline:** AUDIT.md (2026-02-18)
**Current PRD Coverage:** ~75-80% (up from 30% at audit time)

---

## Summary

The bulk of the remediation work from AUDIT.md phases 1-6 has been completed. What remains falls into three categories:

1. **Blocking issues** — 2 items that prevent the system from running end-to-end
2. **Security gaps** — 1 critical item (JWT algorithm) plus hardening
3. **Feature completion** — voice emotion model, Android biometrics, production readiness

---

## Phase A: Blocking Fixes (Priority: CRITICAL)

These must be resolved before any integration testing is possible.

### A-1: Register SignalR in Program.cs

- **File:** `src/API/DigitalTwin.API/Program.cs`
- **Problem:** `CompanionHub` is fully implemented at `src/API/DigitalTwin.API/Hubs/CompanionHub.cs` but never registered in the DI container or endpoint routing. The mobile app's SignalR client (`mobile/lib/signalr.ts`) will fail to connect.
- **Fix:**
  1. Add `builder.Services.AddSignalR()` in service registration section
  2. Add `app.MapHub<CompanionHub>("/hubs/companion")` in endpoint routing section
  3. Register any SignalR-related services (`ISharedExperienceService`, `IEventBus`) if not already registered
- **Effort:** Small (< 1 hour)
- **Validates:** Real-time messaging, shared experiences, emotion streaming

### A-2: Register Missing Services for New Features

- **File:** `src/API/DigitalTwin.API/Program.cs`
- **Problem:** New controllers (`BiometricController`, `CoachingController`) and their backing services need DI registration verification. Ensure all of the following are registered:
  - `IBiometricService` → `BiometricService`
  - `ICoachingService` → `CoachingService`
  - `ISharedExperienceService` → `SharedExperienceService`
  - `IEventBus` → `RabbitMqEventBus`
- **Fix:** Verify and add any missing `builder.Services.AddScoped<>()` or `AddSingleton<>()` calls
- **Effort:** Small (< 1 hour)

---

## Phase B: Security Hardening (Priority: CRITICAL)

### B-1: Migrate JWT from HS256 to RS256

- **File:** `src/API/DigitalTwin.API/Program.cs` (line ~102)
- **Problem:** JWT token validation still uses `SymmetricSecurityKey` (HS256). The audit flagged this as critical — HS256 with a shared secret is vulnerable to token forgery if the secret leaks.
- **Fix:**
  1. Generate RSA key pair for JWT signing
  2. Replace `SymmetricSecurityKey` with `RsaSecurityKey` in token validation parameters
  3. Update token generation to use RS256 algorithm
  4. Add environment variables: `JwtConfiguration__PublicKeyPath`, `JwtConfiguration__PrivateKeyPath`
  5. Update docker-compose and k8s configs to mount key files
- **Effort:** Medium (2-3 hours)
- **Risk:** Breaking change for any existing tokens — coordinate with deployment

### B-2: File Upload Validation Audit

- **Files:** `services/voice-service/main.py`, `services/avatar-generation-service/main.py`
- **Problem:** File upload endpoints accept files but comprehensive validation (max file size, MIME type whitelist, filename sanitization) needs verification and hardening.
- **Fix:**
  1. Enforce max file size (10MB for images, 25MB for audio)
  2. Validate MIME types against whitelist (image/png, image/jpeg, audio/wav, audio/webm)
  3. Sanitize filenames to prevent path traversal
  4. Add image decompression bomb protection (max pixel dimensions)
- **Effort:** Small-Medium (1-2 hours)

---

## Phase C: Feature Completion (Priority: HIGH)

### C-1: Voice Emotion Detection Model Training

- **Files:** `services/voice-service/`
- **Problem:** Voice emotion analysis falls back to placeholder/heuristic analysis. The training script exists but was never run. Without this, the multi-modal emotion fusion (`EmotionFusionService`) has no voice signal input.
- **Fix:**
  1. Source training dataset (e.g., RAVDESS, CREMA-D)
  2. Run training pipeline to produce model weights
  3. Integrate trained model into voice-service inference endpoint
  4. Validate accuracy and update confidence scores in fusion
- **Effort:** Large (4-8 hours)
- **Dependency:** Training data + GPU compute

### C-2: Android Biometric Integration (Google Fit)

- **File:** `mobile/lib/biometric.ts` (lines ~97-104)
- **Problem:** iOS HealthKit integration is functional but Android Google Fit integration is a stub returning placeholder data.
- **Fix:**
  1. Implement Google Fit REST API client using OAuth2 scopes
  2. Map Google Fit data types to `BiometricReading` entity (heart rate, steps, sleep)
  3. Add Android-specific permissions in `app.json`
  4. Test on physical Android device
- **Effort:** Medium (3-4 hours)

### C-3: Text Emotion Detection Enhancement

- **Status from audit:** 15% — keyword matching only, no NLP
- **Problem:** Text emotion analysis relies on basic keyword matching. For accurate multi-modal fusion, this should use a proper NLP model.
- **Fix:**
  1. Integrate a pre-trained emotion classification model (e.g., HuggingFace `bhadresh-savani/bert-base-go-emotion`) into the LLM service
  2. Add a `/analyze-emotion` endpoint that returns emotion + confidence
  3. Wire into `EmotionFusionService` text signal pipeline
- **Effort:** Medium (3-4 hours)

---

## Phase D: Integration & Testing (Priority: HIGH)

### D-1: End-to-End Integration Testing

- **Problem:** No integration test suite exists to validate the full flow: mobile app → .NET API → Python microservices → database.
- **Fix:**
  1. Write API integration tests covering: auth flow, conversation lifecycle, emotion fusion, voice upload/TTS
  2. Write mobile E2E tests using Detox or Maestro
  3. Create docker-compose test profile that spins up all services
- **Effort:** Large (8-12 hours)

### D-2: Wire Up Observability Stack

- **Problem from audit:** Prometheus, Grafana, Jaeger, ELK are deployed but emit zero metrics/traces/logs.
- **Current status:** Prometheus metrics endpoints added (Phase 4 fix), but need verification.
- **Fix:**
  1. Verify Prometheus scrape configs target all services
  2. Add custom metrics: request latency, emotion fusion duration, LLM response time, active conversations
  3. Create Grafana dashboard with key SLIs
  4. Add OpenTelemetry tracing spans to critical paths
  5. Configure structured JSON logging to ELK
- **Effort:** Large (6-10 hours)

### D-3: Load Testing

- **Problem:** No performance baseline exists. Redis caching and RabbitMQ message queuing are wired but untested under load.
- **Fix:**
  1. Create k6 or Locust load test scripts
  2. Profile: emotion fusion latency, semantic memory search, concurrent WebSocket connections
  3. Identify bottlenecks and set performance budgets
- **Effort:** Medium (3-4 hours)

---

## Phase E: Production Readiness (Priority: MEDIUM)

### E-1: CI/CD Pipeline Hardening

- **File:** `Dockerfile.ci`, CI configs
- **Problem from audit:** Dockerfile.ci runs as root and pipes URLs to shell.
- **Fix:**
  1. Add non-root USER directive to all Dockerfiles
  2. Pin dependency versions (remove `:latest` tags)
  3. Add multi-stage builds to reduce image size
  4. Add SBOM generation and vulnerability scanning to pipeline
- **Effort:** Medium (2-3 hours)

### E-2: Kubernetes Production Alignment

- **Problem from audit:** K8s configs have several issues — missing deployments for 9 services, wrong secret key references, missing pathType on Ingress.
- **Fix:**
  1. Add k8s deployments for all Python microservices
  2. Fix Ingress `pathType: Prefix` on all rules
  3. Fix ServiceMonitor apiVersion to `monitoring.coreos.com/v1`
  4. Fix Grafana datasource to reference correct Prometheus service name
  5. Verify all secret references match actual secret manifests
- **Effort:** Medium (3-4 hours)

### E-3: Product Identity Finalization

- **Problem from audit (AD-6):** System prompt still partially references building management.
- **Fix:**
  1. Audit all AI system prompts and rewrite for emotional companion identity
  2. Update onboarding flow copy
  3. Remove or deprioritize building management references in UI
- **Effort:** Small (1-2 hours)

---

## Implementation Order

```
Week 1 — Critical Path
├── Phase A: Blocking Fixes (A-1, A-2)              ~2 hours
├── Phase B: Security (B-1, B-2)                     ~4 hours
└── Phase C-3: Text Emotion NLP                      ~4 hours

Week 2 — Feature Completion & Testing
├── Phase C-1: Voice Emotion Model                   ~6 hours
├── Phase C-2: Android Biometrics                    ~4 hours
├── Phase D-1: Integration Tests                     ~10 hours
└── Phase D-3: Load Testing                          ~4 hours

Week 3 — Production Hardening
├── Phase D-2: Observability Wiring                  ~8 hours
├── Phase E-1: CI/CD Hardening                       ~3 hours
├── Phase E-2: K8s Alignment                         ~4 hours
└── Phase E-3: Product Identity                      ~2 hours
```

**Total estimated effort:** ~51 hours (~6-7 working days)

---

## Completion Tracking

| Phase | Item | Status |
|-------|------|--------|
| A-1 | Register SignalR in Program.cs | ✅ DONE |
| A-2 | Register missing DI services | ✅ DONE |
| B-1 | JWT HS256 → RS256 migration | ✅ DONE |
| B-2 | File upload validation | ✅ DONE (already existed) |
| C-1 | Voice emotion model training | ✅ DONE (already existed) |
| C-2 | Android Google Fit integration | ✅ DONE |
| C-3 | Text emotion NLP model | ✅ DONE (already existed) |
| D-1 | End-to-end integration tests | ✅ DONE |
| D-2 | Observability stack wiring | ✅ DONE |
| D-3 | Load testing | ✅ DONE |
| E-1 | CI/CD pipeline hardening | ✅ DONE |
| E-2 | K8s production alignment | ✅ DONE |
| E-3 | Product identity finalization | ✅ DONE |
