# DELTA_FINAL.md — Final Implementation Plan to 100% PRD Coverage

**Date:** 2026-02-19
**Baseline:** UVP_PRD.md gap analysis
**Goal:** Close all remaining PRD gaps and ship a complete product

---

## What's Missing

| # | Feature | PRD Phase | Effort |
|---|---------|-----------|--------|
| F-1 | Personal history & life events system | Phase 2 | Medium |
| F-2 | Family/household accounts | Phase 2 | Medium |
| F-3 | Push notification infrastructure | Cross-cutting | Small |
| F-4 | Onboarding flow (first-time user experience) | Cross-cutting | Small |
| F-5 | Achievement & gamification system | Phase 2 | Small |
| F-6 | Community forums & peer support | Phase 3 | Large |
| F-7 | Creative expression suite | Phase 3 | Large |
| F-8 | Therapist marketplace & clinical screening | Phase 3 | Large |
| F-9 | Content moderation system | Phase 3 | Medium |
| F-10 | Co-learning & education system | Phase 2 | Medium |
| F-11 | Web dashboard | Cross-cutting | Large |

---

## Implementation Phases

### Phase I: Core Gaps (F-1 through F-5)

These are small-to-medium features that complete the Phase 2 PRD vision and round out the user experience. All are self-contained and can be built in parallel.

---

#### F-1: Personal History & Life Events

**Entities:**
- `LifeEvent` — userId, title, description, eventDate, category (career/relationship/health/education/milestone), emotionalImpact, isRecurring
- `PersonalContext` — userId, culturalBackground, communicationPreferences, importantPeople (JSON), values (JSON)

**Interface:** `IPersonalHistoryService`
- `AddLifeEventAsync(userId, event)`
- `GetTimelineAsync(userId, startDate, endDate)`
- `GetUpcomingEventsAsync(userId)` — birthdays, anniversaries, recurring events
- `GetContextForConversationAsync(userId)` — feed life context into LLM prompts

**API:** `PersonalHistoryController`
- `POST /api/personal-history/events`
- `GET /api/personal-history/timeline?start={date}&end={date}`
- `DELETE /api/personal-history/events/{id}`

**Mobile:**
- New tab or profile subsection: "My Life" timeline view
- Add event form with category picker and date selector
- Timeline visualization with emotion markers

**Integration:** Wire into `ConversationService` so the LLM prompt includes relevant life context (upcoming anniversary, recent job change, etc.)

**Files to create/modify:**
- `src/Core/DigitalTwin.Core/Entities/PersonalHistoryEntities.cs` (new)
- `src/Core/DigitalTwin.Core/Interfaces/IPersonalHistoryService.cs` (new)
- `src/Core/DigitalTwin.Core/Services/PersonalHistoryService.cs` (new)
- `src/Core/DigitalTwin.Core/Data/DigitalTwinDbContext.cs` (add DbSets)
- `src/API/DigitalTwin.API/Controllers/PersonalHistoryController.cs` (new)
- `src/API/DigitalTwin.API/Program.cs` (register DI)
- `mobile/lib/api.ts` (add endpoints)
- `mobile/app/life/index.tsx` (new — timeline screen)
- `mobile/app/life/add-event.tsx` (new — add event form)

---

#### F-2: Family/Household Accounts

**Entities:**
- `Family` — id, name, createdByUserId, createdAt
- `FamilyMember` — familyId, userId, role (owner/adult/child), joinedAt

**Interface:** `IFamilyService`
- `CreateFamilyAsync(userId, name)`
- `InviteMemberAsync(familyId, email, role)`
- `AcceptInviteAsync(userId, inviteCode)`
- `GetFamilyMembersAsync(familyId)`
- `GetSharedInsightsAsync(familyId)` — aggregated family emotional trends (anonymized per member preference)

**API:** `FamilyController`
- `POST /api/family`
- `POST /api/family/{id}/invite`
- `POST /api/family/join`
- `GET /api/family/{id}/members`
- `GET /api/family/{id}/insights`

**Mobile:**
- Profile → "My Family" section
- Create/join family flow
- Family insights dashboard (shared emotional trends)

**Files to create/modify:**
- `src/Core/DigitalTwin.Core/Entities/FamilyEntities.cs` (new)
- `src/Core/DigitalTwin.Core/Interfaces/IFamilyService.cs` (new)
- `src/Core/DigitalTwin.Core/Services/FamilyService.cs` (new)
- `src/Core/DigitalTwin.Core/Data/DigitalTwinDbContext.cs` (add DbSets)
- `src/API/DigitalTwin.API/Controllers/FamilyController.cs` (new)
- `src/API/DigitalTwin.API/Program.cs` (register DI)
- `mobile/lib/api.ts` (add endpoints)
- `mobile/app/family/index.tsx` (new)
- `mobile/app/family/invite.tsx` (new)
- `mobile/app/family/insights.tsx` (new)

---

#### F-3: Push Notification Infrastructure

**Backend:**
- `INotificationService` — `SendPushAsync(userId, title, body, data)`
- `NotificationService` — integrates with Expo Push Notifications API (since we're using Expo)
- `DeviceToken` entity — userId, token, platform (ios/android), createdAt
- Wire into `ProactiveCheckInService` to actually deliver check-ins as push notifications
- Wire into `CoachingService` for habit reminders

**API:**
- `POST /api/notifications/register-device` — store device push token
- `DELETE /api/notifications/unregister-device`

**Mobile:**
- `mobile/lib/notifications.ts` — Expo Notifications setup, token registration, notification handlers
- Wire into app startup to request permissions and register token

**Files to create/modify:**
- `src/Core/DigitalTwin.Core/Entities/DeviceToken.cs` (new)
- `src/Core/DigitalTwin.Core/Interfaces/INotificationService.cs` (new)
- `src/Core/DigitalTwin.Core/Services/NotificationService.cs` (new)
- `src/Core/DigitalTwin.Core/Data/DigitalTwinDbContext.cs` (add DbSet)
- `src/API/DigitalTwin.API/Controllers/NotificationController.cs` (new)
- `src/API/DigitalTwin.API/Program.cs` (register DI)
- `src/Core/DigitalTwin.Core/Services/ProactiveCheckInService.cs` (wire push delivery)
- `mobile/lib/notifications.ts` (new)
- `mobile/lib/api.ts` (add endpoints)
- `mobile/app/_layout.tsx` (init notifications on app start)

---

#### F-4: Onboarding Flow

**Mobile screens:**
1. `mobile/app/onboarding/welcome.tsx` — app intro with value prop slides
2. `mobile/app/onboarding/mood-check.tsx` — "How are you feeling right now?" initial emotion capture
3. `mobile/app/onboarding/personalize.tsx` — name your companion, choose voice, set communication style
4. `mobile/app/onboarding/permissions.tsx` — request camera, microphone, notifications, HealthKit
5. `mobile/app/onboarding/complete.tsx` — "You're all set!" → first conversation

**Logic:**
- Store `hasCompletedOnboarding` flag in AsyncStorage
- `_layout.tsx` checks flag on startup and redirects to onboarding if false
- Onboarding data sent to server to initialize AITwinProfile preferences

**Files to create/modify:**
- `mobile/app/onboarding/welcome.tsx` (new)
- `mobile/app/onboarding/mood-check.tsx` (new)
- `mobile/app/onboarding/personalize.tsx` (new)
- `mobile/app/onboarding/permissions.tsx` (new)
- `mobile/app/onboarding/complete.tsx` (new)
- `mobile/app/_layout.tsx` (add onboarding redirect)
- `mobile/lib/api.ts` (add onboarding completion endpoint)

---

#### F-5: Achievement & Gamification System

**Entities:**
- `Achievement` — id, key, title, description, iconUrl, category (emotional/social/growth/consistency), condition (JSON)
- `UserAchievement` — userId, achievementId, unlockedAt, progress (0-100)

**Interface:** `IAchievementService`
- `CheckAndUnlockAsync(userId, eventType, eventData)` — called after key actions
- `GetUserAchievementsAsync(userId)`
- `GetProgressAsync(userId, achievementKey)`

**Built-in achievements:**
- "First Conversation", "7-Day Streak", "Emotion Explorer" (detected 5+ unique emotions), "Memory Keeper" (50+ memories), "Goal Setter", "Journal Regular", "Shared a Room", "Voice Activated", "Avatar Created", "Check-In Champion"

**API:** `AchievementController`
- `GET /api/achievements` — all available
- `GET /api/achievements/mine` — user's unlocked achievements + progress

**Mobile:**
- Profile → "Achievements" section with badge grid
- Toast notification when achievement unlocked

**Files to create/modify:**
- `src/Core/DigitalTwin.Core/Entities/AchievementEntities.cs` (new)
- `src/Core/DigitalTwin.Core/Interfaces/IAchievementService.cs` (new)
- `src/Core/DigitalTwin.Core/Services/AchievementService.cs` (new)
- `src/Core/DigitalTwin.Core/Data/DigitalTwinDbContext.cs` (add DbSets)
- `src/API/DigitalTwin.API/Controllers/AchievementController.cs` (new)
- `src/API/DigitalTwin.API/Program.cs` (register DI)
- `mobile/lib/api.ts` (add endpoints)
- `mobile/app/(tabs)/profile.tsx` (add achievements section)
- `mobile/app/achievements/index.tsx` (new — full badge grid)

---

### Phase II: Social & Community (F-6, F-9)

---

#### F-6: Community Forums & Peer Support

**Entities:**
- `CommunityGroup` — id, name, description, category (support/interest/wellness), isModerated, createdByUserId
- `CommunityPost` — id, groupId, authorUserId, content, createdAt, isAnonymous
- `CommunityReply` — id, postId, authorUserId, content, createdAt
- `CommunityMembership` — groupId, userId, role (member/moderator), joinedAt

**Interface:** `ICommunityService`
- `CreateGroupAsync(userId, name, description, category)`
- `GetGroupsAsync(category?, search?)` — discover groups
- `JoinGroupAsync(userId, groupId)`
- `CreatePostAsync(userId, groupId, content, isAnonymous)`
- `GetPostsAsync(groupId, page, pageSize)`
- `ReplyToPostAsync(userId, postId, content)`
- `GetSuggestedGroupsAsync(userId)` — recommend based on emotional patterns

**API:** `CommunityController`
- Standard CRUD for groups, posts, replies
- `GET /api/community/suggested` — personalized recommendations

**Mobile:**
- New tab or top-level screen: "Community"
- Group discovery with categories
- Post feed with anonymous option
- Reply threads

**Files to create/modify:**
- `src/Core/DigitalTwin.Core/Entities/CommunityEntities.cs` (new)
- `src/Core/DigitalTwin.Core/Interfaces/ICommunityService.cs` (new)
- `src/Core/DigitalTwin.Core/Services/CommunityService.cs` (new)
- `src/Core/DigitalTwin.Core/Data/DigitalTwinDbContext.cs` (add DbSets)
- `src/API/DigitalTwin.API/Controllers/CommunityController.cs` (new)
- `src/API/DigitalTwin.API/Program.cs` (register DI)
- `mobile/lib/api.ts` (add endpoints)
- `mobile/app/community/index.tsx` (new — group list)
- `mobile/app/community/[groupId].tsx` (new — post feed)
- `mobile/app/community/post/[postId].tsx` (new — reply thread)

---

#### F-9: Content Moderation System

**Entities:**
- `ContentReport` — id, reporterUserId, contentType (post/reply/message), contentId, reason (harassment/spam/selfharm/inappropriate), status (pending/reviewed/actioned), reviewedByUserId, createdAt

**Interface:** `IModerationService`
- `ReportContentAsync(userId, contentType, contentId, reason)`
- `AutoModerateAsync(content)` — keyword + ML-based automatic flagging
- `ReviewReportAsync(moderatorId, reportId, action)` — approve/dismiss/ban
- `GetPendingReportsAsync(page, pageSize)`

**Integration:**
- Hook into `CommunityService.CreatePostAsync` and `ReplyToPostAsync` for auto-moderation before publishing
- Extend `SafetyPlugin` patterns for community content
- Add moderator role to RBAC

**Files to create/modify:**
- `src/Core/DigitalTwin.Core/Entities/ModerationEntities.cs` (new)
- `src/Core/DigitalTwin.Core/Interfaces/IModerationService.cs` (new)
- `src/Core/DigitalTwin.Core/Services/ModerationService.cs` (new)
- `src/Core/DigitalTwin.Core/Data/DigitalTwinDbContext.cs` (add DbSet)
- `src/API/DigitalTwin.API/Controllers/ModerationController.cs` (new)
- `src/API/DigitalTwin.API/Program.cs` (register DI)
- `mobile/lib/api.ts` (add report endpoint)

---

### Phase III: Premium Features (F-7, F-8, F-10)

---

#### F-7: Creative Expression Suite

**Entities:**
- `CreativeWork` — id, userId, type (story/poem/reflection/gratitude), title, content (JSON for structured content), mood, isShared, createdAt
- `CollaborativeStory` — id, roomId, title, participants (JSON), chapters (JSON)

**Interface:** `ICreativeService`
- `CreateWorkAsync(userId, type, title, content, mood)`
- `GetWorksAsync(userId, type?)` — user's creative portfolio
- `ShareWorkAsync(userId, workId, groupId?)` — share to community
- `StartCollaborativeStoryAsync(roomId, title)` — story in shared room
- `AddChapterAsync(storyId, userId, content)` — add to collaborative story
- `GeneratePromptAsync(userId, type)` — AI-generated creative prompts based on mood

**API:** `CreativeController`
- CRUD for creative works
- `POST /api/creative/prompt` — get AI creative prompt
- `POST /api/creative/collaborative` — start/continue collaborative story

**Mobile:**
- "Create" screen accessible from chat or profile
- Writing editor with mood tagging
- Portfolio view of past creations
- Collaborative story mode in shared rooms

**Files to create/modify:**
- `src/Core/DigitalTwin.Core/Entities/CreativeEntities.cs` (new)
- `src/Core/DigitalTwin.Core/Interfaces/ICreativeService.cs` (new)
- `src/Core/DigitalTwin.Core/Services/CreativeService.cs` (new)
- `src/Core/DigitalTwin.Core/Data/DigitalTwinDbContext.cs` (add DbSets)
- `src/API/DigitalTwin.API/Controllers/CreativeController.cs` (new)
- `src/API/DigitalTwin.API/Program.cs` (register DI)
- `mobile/lib/api.ts` (add endpoints)
- `mobile/app/creative/index.tsx` (new — portfolio)
- `mobile/app/creative/editor.tsx` (new — writing editor)
- `mobile/app/creative/prompts.tsx` (new — AI prompts)

---

#### F-8: Therapist Marketplace & Clinical Screening

**Entities:**
- `TherapistProfile` — id, userId, name, credentials, specializations (JSON), availability (JSON), ratePerSession, isVerified
- `TherapySession` — id, therapistId, clientUserId, scheduledAt, duration, status (scheduled/completed/cancelled), notes
- `ClinicalScreening` — id, userId, type (PHQ9/GAD7/custom), responses (JSON), score, severity, completedAt
- `TherapistReferral` — id, userId, reason, urgency, createdAt

**Interface:** `ITherapyService`
- `GetTherapistsAsync(specialization?, availability?)`
- `BookSessionAsync(userId, therapistId, dateTime)`
- `ConductScreeningAsync(userId, type)` — returns questions
- `SubmitScreeningAsync(userId, screeningId, responses)` — scores and flags
- `GenerateReferralAsync(userId, reason)` — AI detects need for professional help

**Integration:**
- `SafetyPlugin` triggers `GenerateReferralAsync` when crisis patterns detected
- `ProactiveCheckInService` can suggest periodic screenings
- Screening scores feed into `EmotionFusionService` as clinical signal

**API:** `TherapyController`
- `GET /api/therapy/therapists`
- `POST /api/therapy/sessions`
- `POST /api/therapy/screening/start`
- `POST /api/therapy/screening/{id}/submit`

**Mobile:**
- Profile → "Professional Support" section
- Therapist directory with booking
- Screening questionnaire flow (PHQ-9, GAD-7)
- Results visualization with recommendations

**Files to create/modify:**
- `src/Core/DigitalTwin.Core/Entities/TherapyEntities.cs` (new)
- `src/Core/DigitalTwin.Core/Interfaces/ITherapyService.cs` (new)
- `src/Core/DigitalTwin.Core/Services/TherapyService.cs` (new)
- `src/Core/DigitalTwin.Core/Data/DigitalTwinDbContext.cs` (add DbSets)
- `src/API/DigitalTwin.API/Controllers/TherapyController.cs` (new)
- `src/API/DigitalTwin.API/Program.cs` (register DI)
- `mobile/lib/api.ts` (add endpoints)
- `mobile/app/therapy/index.tsx` (new — therapist directory)
- `mobile/app/therapy/screening.tsx` (new — questionnaire)
- `mobile/app/therapy/book.tsx` (new — booking flow)

---

#### F-10: Co-Learning & Education System

**Entities:**
- `LearningPath` — id, title, description, category (emotional-intelligence/mindfulness/communication/stress-management), modules (JSON), estimatedDuration
- `UserLearningProgress` — userId, pathId, currentModuleIndex, completedModules (JSON), startedAt, completedAt
- `LearningModule` — id, pathId, title, content (JSON — text + exercises + reflection prompts), order

**Interface:** `ILearningService`
- `GetPathsAsync(category?)` — browse available learning paths
- `StartPathAsync(userId, pathId)`
- `GetCurrentModuleAsync(userId, pathId)` — current lesson
- `CompleteModuleAsync(userId, pathId, reflectionNotes)` — mark done + journal reflection
- `GetProgressAsync(userId)` — all paths in progress
- `SuggestPathAsync(userId)` — AI recommends based on emotional patterns

**Built-in paths:**
- "Understanding Your Emotions" (emotional literacy)
- "Mindfulness Basics" (meditation, breathing)
- "Better Communication" (active listening, assertiveness)
- "Stress Management Toolkit" (coping strategies)
- "Building Resilience" (growth mindset)

**API:** `LearningController`
- `GET /api/learning/paths`
- `POST /api/learning/paths/{id}/start`
- `GET /api/learning/progress`
- `POST /api/learning/paths/{pathId}/modules/{moduleIndex}/complete`
- `GET /api/learning/suggested`

**Mobile:**
- "Learn" screen accessible from profile or a new tab
- Path browser with categories
- Module viewer with exercises and reflection
- Progress dashboard

**Files to create/modify:**
- `src/Core/DigitalTwin.Core/Entities/LearningEntities.cs` (new)
- `src/Core/DigitalTwin.Core/Interfaces/ILearningService.cs` (new)
- `src/Core/DigitalTwin.Core/Services/LearningService.cs` (new)
- `src/Core/DigitalTwin.Core/Data/DigitalTwinDbContext.cs` (add DbSets)
- `src/API/DigitalTwin.API/Controllers/LearningController.cs` (new)
- `src/API/DigitalTwin.API/Program.cs` (register DI)
- `mobile/lib/api.ts` (add endpoints)
- `mobile/app/learn/index.tsx` (new — path browser)
- `mobile/app/learn/[pathId].tsx` (new — module viewer)
- `mobile/app/learn/progress.tsx` (new — progress dashboard)

---

### Phase IV: Web Dashboard (F-11)

#### F-11: Web Dashboard (Admin + User Analytics)

A lightweight Next.js web dashboard for:
- **User view:** Emotional trends, conversation history, learning progress, achievements — desktop-friendly version of mobile insights
- **Admin view:** User management, moderation queue, system analytics, therapist verification

This is deferred to last because the mobile app serves all user-facing needs. The web dashboard is primarily for admin operations and power users who prefer desktop.

**Files to create:**
- `web/` — Next.js 14 project with App Router
- `web/app/page.tsx` — dashboard home
- `web/app/insights/page.tsx` — emotional analytics
- `web/app/admin/users/page.tsx` — user management
- `web/app/admin/moderation/page.tsx` — content moderation queue
- `web/app/admin/analytics/page.tsx` — system metrics (embed Grafana)

---

## Implementation Order

```
Phase I: Core Gaps — parallel implementation
├── F-3: Push notifications          ~2 hours
├── F-4: Onboarding flow             ~2 hours
├── F-5: Achievements/gamification   ~3 hours
├── F-1: Personal history/life events ~4 hours
└── F-2: Family/household accounts   ~4 hours
                                     ─────────
                              Total: ~15 hours (parallel: ~5 hours)

Phase II: Social & Community
├── F-6: Community forums            ~6 hours
└── F-9: Content moderation          ~3 hours
                                     ─────────
                              Total: ~9 hours (parallel: ~6 hours)

Phase III: Premium Features
├── F-10: Co-learning system         ~4 hours
├── F-7: Creative expression         ~5 hours
└── F-8: Therapist marketplace       ~6 hours
                                     ─────────
                              Total: ~15 hours (parallel: ~6 hours)

Phase IV: Web Dashboard
└── F-11: Next.js admin + user dashboard  ~8 hours
                                     ─────────
                              Total: ~8 hours
```

**Grand total: ~47 hours of implementation**
**With parallel agents: ~25 hours of wall clock time**

---

## Completion Tracking

| Phase | Item | Status |
|-------|------|--------|
| I | F-1: Personal history & life events | ✅ DONE |
| I | F-2: Family/household accounts | ✅ DONE |
| I | F-3: Push notification infrastructure | ✅ DONE |
| I | F-4: Onboarding flow | ✅ DONE |
| I | F-5: Achievement & gamification | ✅ DONE |
| II | F-6: Community forums & peer support | ✅ DONE |
| II | F-9: Content moderation system | ✅ DONE |
| III | F-7: Creative expression suite | ✅ DONE |
| III | F-8: Therapist marketplace & screening | ✅ DONE |
| III | F-10: Co-learning & education | ✅ DONE |
| IV | F-11: Web dashboard | ✅ DONE |
