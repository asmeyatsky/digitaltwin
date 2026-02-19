# E2E.md — End-to-End Consistency Report

**Date:** 2026-02-19
**Scope:** Full cross-layer consistency check across mobile, .NET API, Python microservices, Docker, and Kubernetes configs.

---

## Summary

| Severity | Count | Fixed | Remaining |
|----------|-------|-------|-----------|
| CRITICAL | 20 | 20 | 0 |
| WARNING | 19 | 14 | 5 |
| INFO | 6 | — | — |

---

## CRITICAL Issues

### Mobile-to-API Contract Mismatches

| # | Issue | Files | Status |
|---|-------|-------|--------|
| C-1 | Auth login sends `{email}` but server expects `{Username}` — login always fails | `mobile/lib/api.ts` vs `AuthController.cs` | ✅ FIXED |
| C-2 | Auth response shape mismatch — server returns `Token` not `accessToken`, `UserId` not `id` | same | ✅ FIXED |
| C-3 | Auth refresh sends `{refreshToken}` but server requires `{Token, RefreshToken}` | same | ✅ FIXED |
| C-4 | Conversation message sends `{content}` but server expects `{Message}` | `api.ts` vs `ConversationController.cs` | ✅ FIXED |
| C-5 | Conversation start sends empty body but server requires `{Message}` | same | ✅ FIXED |
| C-6 | `GET /api/conversation/list` doesn't exist on server | `api.ts` — no matching action | ✅ FIXED — removed from mobile |
| C-7 | `DELETE /api/conversation/{id}` doesn't exist on server | same | ✅ FIXED — removed from mobile |
| C-8 | `GET /api/user/profile` and `PATCH /api/user/profile` — no UserController | `api.ts` — controller missing | ✅ FIXED — created UserController |
| C-9 | `GET /api/insights/emotions` — no InsightsController | `api.ts` — controller missing | ✅ FIXED — created InsightsController |

### Server-to-Python Service Path Mismatches

| # | Issue | Files | Status |
|---|-------|-------|--------|
| C-10 | EmotionController calls `POST /analyze-face` but DeepFace has `/analyze/facial-expression` | `EmotionController.cs` vs `deepface-service/main.py` | ✅ FIXED |
| C-11 | AvatarController calls `POST /generate` but avatar service has `/avatar/generate` | `AvatarController.cs` vs `avatar-generation-service/main.py` | ✅ FIXED |

### Infrastructure

| # | Issue | Files | Status |
|---|-------|-------|--------|
| C-12 | `DeepFace__BaseUrl` in docker-compose vs `Services__DeepFace__BaseUrl` in Program.cs — env var name mismatch | `docker-compose.yml` vs `Program.cs` | ✅ FIXED |
| C-13 | Missing `Services__Avatar__BaseUrl` and `Services__Voice__BaseUrl` in docker-compose | same | ✅ FIXED |
| C-14 | `JwtConfiguration__SecretKey` set in docker-compose but code reads `PrivateKeyPath`/`PublicKeyPath` | same | ✅ FIXED |
| C-15 | `Dockerfile.dev` referenced by docker-compose.dev.yml does not exist | `docker-compose.dev.yml` | ✅ FIXED — created Dockerfile.dev |
| C-16 | LLM service has no Dockerfile | `services/llm-service/` | ✅ FIXED — created Dockerfile |
| C-17 | k8s Redis connection string uses hostname `redis` but service is `digitaltwin-redis-service`, no password | `k8s/api-deployment.yaml` | ✅ FIXED |
| C-18 | k8s network policy labels (`app: postgres`) don't match pod labels (`app: digitaltwin-postgres`) | `k8s/network-policy.yaml` | ✅ FIXED |
| C-19 | k8s network policy missing port 8004 (LLM) | same | ✅ FIXED |

### Code

| # | Issue | Files | Status |
|---|-------|-------|--------|
| C-20 | `ConversationSession` entity has no DbSet — conversations can't be persisted | `EmotionalEntities.cs` vs `DigitalTwinDbContext.cs` | ✅ FIXED — added DbSets + model config |

---

## WARNING Issues

| # | Issue | Status |
|---|-------|--------|
| W-1 | `System.Text.Json 8.0.4` has known high-severity vulnerability (NU1903) | ✅ FIXED — bumped to 8.0.5 |
| W-2 | Orphaned `Building/Floor/Room/Equipment/Sensor` DbSets — never used by any service | ✅ FIXED — removed orphaned DbSets |
| W-3 | `ConversationController` bypasses `IConversationService`, uses `DbContext` + raw HTTP directly | ⬜ DEFERRED — refactor requires broader changes |
| W-4 | ~30+ uses of obsolete `EmotionType`/`EmotionalTone` enums — migration incomplete | ⬜ DEFERRED — large-scope enum migration |
| W-5 | Duplicate `PasswordHasher` classes in `Core.Security` (instance) and `Infrastructure.Security` (static) | ✅ FIXED — deleted unused Infrastructure.Security static duplicate; Core.Security instance version is the canonical one (DI-registered) |
| W-6 | `ConversationMessage` and `ConversationMemory` entities have no DbSets | ✅ FIXED — added DbSets + model config |
| W-7 | Biometric data collected on device (`biometric.ts`) is never synced to server API | ✅ FIXED — added syncBiometricData() |
| W-8 | CORS policy doesn't allow `PATCH` method | ✅ FIXED — added PATCH to WithMethods |
| W-9 | `Services__ServiceKey` not set in docker-compose for .NET API | ✅ FIXED |
| W-10 | Multiple Stripe env vars missing from docker-compose | ✅ FIXED |
| W-11 | k8s `DeepFace__BaseUrl` env var uses wrong name (should be `Services__DeepFace__BaseUrl`) | ✅ FIXED |
| W-12 | k8s microservice deployments lack `prometheus.io/scrape: "true"` annotation | ✅ FIXED |
| W-13 | k8s grafana ConfigMap missing Jaeger datasource and dashboard JSON | ⬜ DEFERRED — k8s ConfigMap needs manual provisioning |
| W-14 | Conversation history response shape mismatch between mobile and server | ✅ FIXED — aligned in api.ts rewrite |
| W-15 | Docker-compose dev stack missing connection strings for postgres/redis | ✅ FIXED |
| W-16 | Dockerfile references non-existent `DigitalTwin.Presentation` and `DigitalTwin.Infrastructure.Tests` projects | ✅ FIXED — removed invalid refs |
| W-17 | NuGet version resolution: `Pgvector 0.2.1` → `0.3.0`, `Stripe.net 45.15.0` → `46.0.0` | ⬜ DEFERRED — auto-resolved, no runtime issue |
| W-18 | Test factory doesn't seed biometric/coaching data or register `IEncryptionService` stub | ✅ FIXED |
| W-19 | No SignalR hub integration tests | ⬜ DEFERRED — requires WebSocket test infrastructure |

---

## Verified Correct

- Solution builds with 0 errors
- All DI registrations are valid (interfaces → implementations all match)
- All controller injections are satisfied
- SignalR hub (`/hubs/companion`) methods match mobile `signalr.ts` exactly
- Voice service proxy paths (`VoiceController` → `voice-service/main.py`) all match
- Port assignments consistent across docker-compose and Python services
- Test project compiles and solution file correctly wired
- Mobile API client types match server DTOs and controller routes
- Docker-compose env vars match Program.cs variable names
- k8s network policies reference correct pod labels
