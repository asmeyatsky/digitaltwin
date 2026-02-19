import { useAuthStore } from "./store";

const API_BASE =
  process.env.EXPO_PUBLIC_API_URL || "http://localhost:8080";

// ---------------------------------------------------------------------------
// Types
// ---------------------------------------------------------------------------

export interface ApiResponse<T> {
  success: boolean;
  data: T;
  message?: string;
  errors?: string[];
}

// --- Auth DTOs (match server AuthController.cs) ---

export interface AuthTokens {
  accessToken: string;
  refreshToken: string;
  expiresIn: number;
}

export interface UserProfile {
  id: string;
  username: string;
  roles: string[];
}

export interface LoginRequest {
  Username: string;
  Password: string;
}

export interface RegisterRequest {
  Username: string;
  Email: string;
  Password: string;
  FirstName: string;
  LastName: string;
}

/** Raw shape returned by POST /api/auth/login */
interface LoginResponseRaw {
  Success: boolean;
  Message?: string;
  Token?: string;
  RefreshToken?: string;
  ExpiresIn: number;
  User?: {
    UserId: string;
    Username: string;
    Roles: string[];
  };
}

/** Raw shape returned by POST /api/auth/register */
interface RegisterResponseRaw {
  Success: boolean;
  Message?: string;
  Token?: string;
  RefreshToken?: string;
  User?: {
    UserId: string;
    Username: string;
    Roles: string[];
  };
  Errors?: string[];
}

/** Raw shape returned by POST /api/auth/refresh */
interface TokenRefreshResponseRaw {
  Success: boolean;
  Message?: string;
  Token?: string;
  RefreshToken?: string;
  ExpiresIn: number;
}

// --- Conversation DTOs (match server ConversationController.cs) ---

export interface ConversationStartResponse {
  SessionId: string;
  Response: string;
  EmotionalTone: string;
  Timestamp: string;
}

export interface ConversationMessageRequest {
  ConversationId: string;
  Message: string;
}

export interface ConversationMessageResponse {
  Response: string;
  DetectedEmotion: string;
  AIEmotionalTone: string;
  ResponseTime: string;
  ConversationId: string;
}

export interface ConversationMessage {
  Id: string;
  Content: string;
  Response?: string;
  UserEmotion: string;
  AIEmotion?: string;
  Timestamp: string;
  MessageType: string;
}

export interface ConversationHistoryResponse {
  Messages: ConversationMessage[];
  TotalCount: number;
  Page: number;
  PageSize: number;
}

export interface ConversationEndRequest {
  ConversationId: string;
  SessionDuration?: string; // TimeSpan serialised as string
}

// --- Legacy types kept for other parts of the app ---

export interface EmotionResult {
  primary: string;
  confidence: number;
  secondary?: string;
  valence: number; // -1 to 1
  arousal: number; // 0 to 1
}

/** @deprecated Use ConversationMessage instead */
export interface Message {
  id: string;
  conversationId: string;
  role: "user" | "assistant";
  content: string;
  emotion?: EmotionResult;
  timestamp: string;
  audioUrl?: string;
}

/** @deprecated Use ConversationStartResponse instead */
export interface Conversation {
  id: string;
  userId: string;
  title: string;
  createdAt: string;
  updatedAt: string;
  messageCount: number;
  lastEmotion?: string;
}

export interface SendMessageRequest {
  ConversationId: string;
  Message: string;
}

export interface SendMessageResponse {
  Response: string;
  DetectedEmotion: string;
  AIEmotionalTone: string;
  ResponseTime: string;
  ConversationId: string;
}

export interface EmotionInsights {
  emotionDistribution: Record<string, number>;
  moodTimeline: { date: string; valence: number }[];
  sessionCount: number;
  averageDurationMinutes: number;
  topEmotions: { emotion: string; count: number; percentage: number }[];
}

// Voice types
export interface VoiceInfo {
  id: string;
  name: string;
  language: string;
  gender: string;
  previewUrl?: string;
  isCustom: boolean;
}

export interface VoiceCloneStatus {
  id: string;
  status: "processing" | "ready" | "failed";
  voiceId?: string;
  error?: string;
}

export interface TTSRequest {
  text: string;
  voiceId?: string;
}

// Avatar types
export interface AvatarGenerationResponse {
  id: string;
  status: "queued" | "processing" | "completed" | "failed";
}

export interface AvatarStatus {
  id: string;
  status: "queued" | "processing" | "completed" | "failed";
  avatarUrl?: string;
  thumbnailUrl?: string;
  error?: string;
}

// Emotion (facial) types
export interface FacialEmotionResponse {
  emotion: string;
  confidence: number;
  allEmotions: Record<string, number>;
}

// Subscription types
export type SubscriptionTier = "free" | "plus" | "premium";

export interface SubscriptionTierInfo {
  tier: SubscriptionTier;
  name: string;
  price: number;
  interval: "month";
  features: string[];
  stripePriceId?: string;
}

export interface SubscriptionInfo {
  id: string;
  tier: SubscriptionTier;
  status: "active" | "canceled" | "past_due" | "trialing";
  currentPeriodEnd: string;
  cancelAtPeriodEnd: boolean;
  stripeCustomerId: string;
}

export interface CheckoutSessionResponse {
  sessionId: string;
  clientSecret: string;
  url?: string;
}

// ---------------------------------------------------------------------------
// HTTP helpers
// ---------------------------------------------------------------------------

async function request<T>(
  path: string,
  options: RequestInit = {}
): Promise<ApiResponse<T>> {
  const token = useAuthStore.getState().token;

  const headers: Record<string, string> = {
    "Content-Type": "application/json",
    ...(options.headers as Record<string, string>),
  };

  if (token) {
    headers["Authorization"] = `Bearer ${token}`;
  }

  const res = await fetch(`${API_BASE}${path}`, {
    ...options,
    headers,
  });

  if (res.status === 401) {
    // Attempt a silent refresh
    const refreshed = await silentRefresh();
    if (refreshed) {
      headers["Authorization"] = `Bearer ${useAuthStore.getState().token}`;
      const retry = await fetch(`${API_BASE}${path}`, { ...options, headers });
      return retry.json() as Promise<ApiResponse<T>>;
    }
    useAuthStore.getState().logout();
    throw new Error("Session expired. Please log in again.");
  }

  if (!res.ok) {
    const body = await res.json().catch(() => ({}));
    throw new Error(
      (body as ApiResponse<unknown>).message ??
        `Request failed with status ${res.status}`
    );
  }

  return res.json() as Promise<ApiResponse<T>>;
}

async function silentRefresh(): Promise<boolean> {
  const store = useAuthStore.getState();
  const currentToken = store.token;
  const currentRefreshToken = store.refreshToken;
  if (!currentToken || !currentRefreshToken) return false;

  try {
    const res = await fetch(`${API_BASE}/api/auth/refresh`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({
        Token: currentToken,
        RefreshToken: currentRefreshToken,
      }),
    });

    if (!res.ok) return false;

    const body = (await res.json()) as TokenRefreshResponseRaw;
    if (body.Success && body.Token && body.RefreshToken) {
      useAuthStore.getState().setTokens(body.Token, body.RefreshToken);
      return true;
    }
    return false;
  } catch {
    return false;
  }
}

async function requestRaw(
  path: string,
  options: RequestInit = {}
): Promise<Response> {
  const token = useAuthStore.getState().token;

  const headers: Record<string, string> = {
    ...(options.headers as Record<string, string>),
  };

  if (token) {
    headers["Authorization"] = `Bearer ${token}`;
  }

  const res = await fetch(`${API_BASE}${path}`, {
    ...options,
    headers,
  });

  if (res.status === 401) {
    const refreshed = await silentRefresh();
    if (refreshed) {
      headers["Authorization"] = `Bearer ${useAuthStore.getState().token}`;
      return fetch(`${API_BASE}${path}`, { ...options, headers });
    }
    useAuthStore.getState().logout();
    throw new Error("Session expired. Please log in again.");
  }

  if (!res.ok) {
    throw new Error(`Request failed with status ${res.status}`);
  }

  return res;
}

async function requestMultipart<T>(
  path: string,
  formData: FormData
): Promise<ApiResponse<T>> {
  const token = useAuthStore.getState().token;

  const headers: Record<string, string> = {};
  if (token) {
    headers["Authorization"] = `Bearer ${token}`;
  }

  const res = await fetch(`${API_BASE}${path}`, {
    method: "POST",
    headers,
    body: formData,
  });

  if (res.status === 401) {
    const refreshed = await silentRefresh();
    if (refreshed) {
      headers["Authorization"] = `Bearer ${useAuthStore.getState().token}`;
      const retry = await fetch(`${API_BASE}${path}`, {
        method: "POST",
        headers,
        body: formData,
      });
      return retry.json() as Promise<ApiResponse<T>>;
    }
    useAuthStore.getState().logout();
    throw new Error("Session expired. Please log in again.");
  }

  if (!res.ok) {
    const body = await res.json().catch(() => ({}));
    throw new Error(
      (body as ApiResponse<unknown>).message ??
        `Request failed with status ${res.status}`
    );
  }

  return res.json() as Promise<ApiResponse<T>>;
}

// ---------------------------------------------------------------------------
// Auth API
// ---------------------------------------------------------------------------

/**
 * Login — sends {Username, Password} and maps the server response
 * (LoginResponse with PascalCase fields) into the mobile AuthTokens + UserProfile shape.
 */
export async function login(
  username: string,
  password: string
): Promise<ApiResponse<AuthTokens & { user: UserProfile }>> {
  const res = await fetch(`${API_BASE}/api/auth/login`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ Username: username, Password: password } satisfies LoginRequest),
  });

  const raw: LoginResponseRaw = await res.json();

  if (!res.ok || !raw.Success) {
    throw new Error(raw.Message ?? `Login failed with status ${res.status}`);
  }

  return {
    success: true,
    data: {
      accessToken: raw.Token!,
      refreshToken: raw.RefreshToken!,
      expiresIn: raw.ExpiresIn,
      user: {
        id: raw.User!.UserId,
        username: raw.User!.Username,
        roles: raw.User!.Roles,
      },
    },
  };
}

/**
 * Register — sends {Username, Email, Password, FirstName, LastName}
 * and maps the server RegisterResponse into mobile types.
 */
export async function register(
  req: RegisterRequest
): Promise<ApiResponse<AuthTokens & { user: UserProfile }>> {
  const res = await fetch(`${API_BASE}/api/auth/register`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(req),
  });

  const raw: RegisterResponseRaw = await res.json();

  if (!res.ok || !raw.Success) {
    const err: ApiResponse<never> = {
      success: false,
      data: undefined as never,
      message: raw.Message ?? `Registration failed with status ${res.status}`,
      errors: raw.Errors,
    };
    throw Object.assign(new Error(err.message!), { response: err });
  }

  return {
    success: true,
    data: {
      accessToken: raw.Token!,
      refreshToken: raw.RefreshToken!,
      expiresIn: 3600,
      user: {
        id: raw.User!.UserId,
        username: raw.User!.Username,
        roles: raw.User!.Roles,
      },
    },
  };
}

/**
 * Refresh token — sends {Token, RefreshToken} per server's TokenRefreshRequest.
 */
export async function refreshToken(
  token: string,
  refresh: string
): Promise<ApiResponse<AuthTokens>> {
  const res = await fetch(`${API_BASE}/api/auth/refresh`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ Token: token, RefreshToken: refresh }),
  });

  const raw: TokenRefreshResponseRaw = await res.json();

  if (!res.ok || !raw.Success) {
    throw new Error(raw.Message ?? `Token refresh failed with status ${res.status}`);
  }

  return {
    success: true,
    data: {
      accessToken: raw.Token!,
      refreshToken: raw.RefreshToken!,
      expiresIn: raw.ExpiresIn,
    },
  };
}

// ---------------------------------------------------------------------------
// Conversation API
// ---------------------------------------------------------------------------

/**
 * Start a new conversation session.
 * Server requires { Message } in the body (ConversationStartRequest).
 * Response is wrapped in ApiResponse<ConversationStartResponse>.
 */
export async function startConversation(
  message: string
): Promise<ApiResponse<ConversationStartResponse>> {
  return request<ConversationStartResponse>("/api/conversation/start", {
    method: "POST",
    body: JSON.stringify({ Message: message }),
  });
}

/**
 * Send a message within an existing conversation.
 * Server expects { ConversationId, Message } (ConversationMessageRequest).
 * Response is wrapped in ApiResponse<ConversationMessageResponse>.
 */
export async function sendMessage(
  conversationId: string,
  message: string
): Promise<ApiResponse<ConversationMessageResponse>> {
  return request<ConversationMessageResponse>("/api/conversation/message", {
    method: "POST",
    body: JSON.stringify({
      ConversationId: conversationId,
      Message: message,
    } satisfies SendMessageRequest),
  });
}

/**
 * Get conversation history with pagination.
 * Server endpoint: GET /api/conversation/history/{conversationId}?page=&pageSize=
 * Response is wrapped in ApiResponse<ConversationHistoryResponse>.
 */
export async function getConversationHistory(
  conversationId: string,
  page: number = 1,
  pageSize: number = 50
): Promise<ApiResponse<ConversationHistoryResponse>> {
  return request<ConversationHistoryResponse>(
    `/api/conversation/history/${conversationId}?page=${page}&pageSize=${pageSize}`
  );
}

/**
 * End a conversation session.
 * Server endpoint: POST /api/conversation/end
 * There is no DELETE endpoint on the server; use this to close a session.
 */
export async function endConversation(
  conversationId: string,
  sessionDuration?: string
): Promise<ApiResponse<void>> {
  return request<void>("/api/conversation/end", {
    method: "POST",
    body: JSON.stringify({
      ConversationId: conversationId,
      SessionDuration: sessionDuration,
    } satisfies ConversationEndRequest),
  });
}

// ---------------------------------------------------------------------------
// Insights API
// ---------------------------------------------------------------------------

export async function getEmotionInsights(
  period: "week" | "month" | "all" = "week"
): Promise<ApiResponse<EmotionInsights>> {
  return request<EmotionInsights>(`/api/insights/emotions?period=${period}`);
}

// ---------------------------------------------------------------------------
// Voice API
// ---------------------------------------------------------------------------

export async function listVoices(): Promise<ApiResponse<VoiceInfo[]>> {
  return request<VoiceInfo[]>("/api/voice/list");
}

export async function getUserVoice(): Promise<ApiResponse<VoiceInfo | null>> {
  return request<VoiceInfo | null>("/api/voice/user/me");
}

export async function textToSpeech(req: TTSRequest): Promise<Blob> {
  const res = await requestRaw("/api/voice/tts", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(req),
  });
  return res.blob();
}

export async function cloneVoice(
  formData: FormData
): Promise<ApiResponse<VoiceCloneStatus>> {
  return requestMultipart<VoiceCloneStatus>("/api/voice/clone", formData);
}

export async function speechToText(
  audioUri: string
): Promise<ApiResponse<{ text: string; language: string }>> {
  const formData = new FormData();
  formData.append("file", {
    uri: audioUri,
    type: "audio/wav",
    name: "recording.wav",
  } as any);
  return requestMultipart<{ text: string; language: string }>(
    "/api/voice/stt",
    formData
  );
}

// ---------------------------------------------------------------------------
// Avatar API
// ---------------------------------------------------------------------------

export async function generateAvatar(
  formData: FormData
): Promise<ApiResponse<AvatarGenerationResponse>> {
  return requestMultipart<AvatarGenerationResponse>(
    "/api/avatar/generate",
    formData
  );
}

export async function getAvatarStatus(
  id: string
): Promise<ApiResponse<AvatarStatus>> {
  return request<AvatarStatus>(`/api/avatar/${id}/status`);
}

export async function getAvatarDownloadUrl(id: string): Promise<string> {
  return `${API_BASE}/api/avatar/${id}/download`;
}

export async function deleteAvatar(
  id: string
): Promise<ApiResponse<void>> {
  return request<void>(`/api/avatar/${id}`, { method: "DELETE" });
}

// ---------------------------------------------------------------------------
// Facial Emotion API
// ---------------------------------------------------------------------------

export async function analyzeEmotion(
  formData: FormData
): Promise<ApiResponse<FacialEmotionResponse>> {
  return requestMultipart<FacialEmotionResponse>(
    "/api/emotion/analyze",
    formData
  );
}

// ---------------------------------------------------------------------------
// Subscription API
// ---------------------------------------------------------------------------

export async function getSubscriptionTiers(): Promise<
  ApiResponse<SubscriptionTierInfo[]>
> {
  return request<SubscriptionTierInfo[]>("/api/subscription/tiers");
}

export async function getSubscription(): Promise<
  ApiResponse<SubscriptionInfo | null>
> {
  return request<SubscriptionInfo | null>("/api/subscription/current");
}

export async function createCheckoutSession(
  tier: SubscriptionTier,
  platform: "web" | "mobile"
): Promise<ApiResponse<CheckoutSessionResponse>> {
  return request<CheckoutSessionResponse>("/api/subscription/checkout", {
    method: "POST",
    body: JSON.stringify({ tier, platform }),
  });
}

export async function cancelSubscription(): Promise<ApiResponse<void>> {
  return request<void>("/api/subscription/cancel", { method: "POST" });
}

// ---------------------------------------------------------------------------
// Check-In API
// ---------------------------------------------------------------------------

export interface CheckInRecord {
  id: string;
  userId: string;
  scheduledAt: string;
  sentAt?: string;
  type: "daily" | "weekly" | "mood_triggered";
  emotionContext?: string;
  response?: string;
  createdAt: string;
}

export interface CheckInSuggestion {
  type: string;
  message: string;
  emotionContext?: string;
  suggestedAt: string;
}

export async function getPendingCheckIns(): Promise<
  ApiResponse<CheckInRecord[]>
> {
  return request<CheckInRecord[]>("/api/checkin/pending");
}

export async function respondToCheckIn(
  id: string,
  response: string
): Promise<ApiResponse<void>> {
  return request<void>(`/api/checkin/${id}/respond`, {
    method: "POST",
    body: JSON.stringify({ response }),
  });
}

export async function evaluateCheckIn(): Promise<
  ApiResponse<CheckInSuggestion | null>
> {
  return request<CheckInSuggestion | null>("/api/checkin/evaluate", {
    method: "POST",
  });
}
