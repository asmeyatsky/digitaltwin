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

export interface AuthTokens {
  accessToken: string;
  refreshToken: string;
  expiresIn: number;
}

export interface UserProfile {
  id: string;
  email: string;
  displayName: string;
  avatarUrl?: string;
  createdAt: string;
  subscriptionTier: "free" | "premium" | "enterprise";
}

export interface LoginRequest {
  email: string;
  password: string;
}

export interface RegisterRequest {
  email: string;
  password: string;
  displayName: string;
}

export interface Conversation {
  id: string;
  userId: string;
  title: string;
  createdAt: string;
  updatedAt: string;
  messageCount: number;
  lastEmotion?: string;
}

export interface Message {
  id: string;
  conversationId: string;
  role: "user" | "assistant";
  content: string;
  emotion?: EmotionResult;
  timestamp: string;
  audioUrl?: string;
}

export interface EmotionResult {
  primary: string;
  confidence: number;
  secondary?: string;
  valence: number; // -1 to 1
  arousal: number; // 0 to 1
}

export interface SendMessageRequest {
  conversationId: string;
  content: string;
  audioBase64?: string;
}

export interface SendMessageResponse {
  userMessage: Message;
  assistantMessage: Message;
  emotion: EmotionResult;
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
  const refreshToken = useAuthStore.getState().refreshToken;
  if (!refreshToken) return false;

  try {
    const res = await fetch(`${API_BASE}/api/auth/refresh`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ refreshToken }),
    });

    if (!res.ok) return false;

    const body = (await res.json()) as ApiResponse<AuthTokens>;
    if (body.success) {
      useAuthStore.getState().setTokens(body.data.accessToken, body.data.refreshToken);
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

export async function login(
  email: string,
  password: string
): Promise<ApiResponse<AuthTokens & { user: UserProfile }>> {
  return request<AuthTokens & { user: UserProfile }>("/api/auth/login", {
    method: "POST",
    body: JSON.stringify({ email, password } satisfies LoginRequest),
  });
}

export async function register(
  email: string,
  password: string,
  displayName: string
): Promise<ApiResponse<AuthTokens & { user: UserProfile }>> {
  return request<AuthTokens & { user: UserProfile }>("/api/auth/register", {
    method: "POST",
    body: JSON.stringify({ email, password, displayName } satisfies RegisterRequest),
  });
}

export async function refreshToken(
  token: string
): Promise<ApiResponse<AuthTokens>> {
  return request<AuthTokens>("/api/auth/refresh", {
    method: "POST",
    body: JSON.stringify({ refreshToken: token }),
  });
}

export async function getProfile(): Promise<ApiResponse<UserProfile>> {
  return request<UserProfile>("/api/user/profile");
}

export async function updateProfile(
  data: Partial<Pick<UserProfile, "displayName" | "avatarUrl">>
): Promise<ApiResponse<UserProfile>> {
  return request<UserProfile>("/api/user/profile", {
    method: "PATCH",
    body: JSON.stringify(data),
  });
}

// ---------------------------------------------------------------------------
// Conversation API
// ---------------------------------------------------------------------------

export async function startConversation(): Promise<ApiResponse<Conversation>> {
  return request<Conversation>("/api/conversation/start", { method: "POST" });
}

export async function listConversations(): Promise<ApiResponse<Conversation[]>> {
  return request<Conversation[]>("/api/conversation/list");
}

export async function getConversationHistory(
  conversationId: string
): Promise<ApiResponse<Message[]>> {
  return request<Message[]>(`/api/conversation/history/${conversationId}`);
}

export async function sendMessage(
  payload: SendMessageRequest
): Promise<ApiResponse<SendMessageResponse>> {
  return request<SendMessageResponse>("/api/conversation/message", {
    method: "POST",
    body: JSON.stringify(payload),
  });
}

export async function deleteConversation(
  conversationId: string
): Promise<ApiResponse<void>> {
  return request<void>(`/api/conversation/${conversationId}`, {
    method: "DELETE",
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
