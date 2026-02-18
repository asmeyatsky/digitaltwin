import { create } from "zustand";
import { Platform } from "react-native";
import type {
  AuthTokens,
  Conversation,
  EmotionResult,
  Message,
  UserProfile,
  SubscriptionTier,
} from "./api";

// ---------------------------------------------------------------------------
// Secure storage wrapper (falls back to memory on web)
// ---------------------------------------------------------------------------

let SecureStore: typeof import("expo-secure-store") | null = null;

async function loadSecureStore() {
  if (Platform.OS !== "web") {
    SecureStore = await import("expo-secure-store");
  }
}

// In-memory fallback for web
const memoryStore = new Map<string, string>();

async function secureSet(key: string, value: string) {
  await loadSecureStore();
  if (SecureStore) {
    await SecureStore.setItemAsync(key, value);
  } else {
    memoryStore.set(key, value);
    try {
      localStorage.setItem(key, value);
    } catch {
      // SSR or no localStorage
    }
  }
}

async function secureGet(key: string): Promise<string | null> {
  await loadSecureStore();
  if (SecureStore) {
    return SecureStore.getItemAsync(key);
  }
  try {
    return localStorage.getItem(key) ?? memoryStore.get(key) ?? null;
  } catch {
    return memoryStore.get(key) ?? null;
  }
}

async function secureDelete(key: string) {
  await loadSecureStore();
  if (SecureStore) {
    await SecureStore.deleteItemAsync(key);
  } else {
    memoryStore.delete(key);
    try {
      localStorage.removeItem(key);
    } catch {
      // SSR or no localStorage
    }
  }
}

// ---------------------------------------------------------------------------
// Auth Store
// ---------------------------------------------------------------------------

interface AuthState {
  token: string | null;
  refreshToken: string | null;
  user: UserProfile | null;
  isAuthenticated: boolean;
  isLoading: boolean;

  setTokens: (access: string, refresh: string) => void;
  setUser: (user: UserProfile) => void;
  loginSuccess: (tokens: AuthTokens, user: UserProfile) => void;
  logout: () => void;
  hydrate: () => Promise<void>;
}

export const useAuthStore = create<AuthState>((set, get) => ({
  token: null,
  refreshToken: null,
  user: null,
  isAuthenticated: false,
  isLoading: true,

  setTokens: (access, refresh) => {
    set({ token: access, refreshToken: refresh, isAuthenticated: true });
    secureSet("dt_access_token", access);
    secureSet("dt_refresh_token", refresh);
  },

  setUser: (user) => set({ user }),

  loginSuccess: (tokens, user) => {
    set({
      token: tokens.accessToken,
      refreshToken: tokens.refreshToken,
      user,
      isAuthenticated: true,
      isLoading: false,
    });
    secureSet("dt_access_token", tokens.accessToken);
    secureSet("dt_refresh_token", tokens.refreshToken);
    secureSet("dt_user", JSON.stringify(user));
  },

  logout: () => {
    set({
      token: null,
      refreshToken: null,
      user: null,
      isAuthenticated: false,
      isLoading: false,
    });
    secureDelete("dt_access_token");
    secureDelete("dt_refresh_token");
    secureDelete("dt_user");
  },

  hydrate: async () => {
    try {
      const [access, refresh, userStr] = await Promise.all([
        secureGet("dt_access_token"),
        secureGet("dt_refresh_token"),
        secureGet("dt_user"),
      ]);

      if (access && refresh) {
        const user = userStr ? (JSON.parse(userStr) as UserProfile) : null;
        set({
          token: access,
          refreshToken: refresh,
          user,
          isAuthenticated: true,
          isLoading: false,
        });
      } else {
        set({ isLoading: false });
      }
    } catch {
      set({ isLoading: false });
    }
  },
}));

// ---------------------------------------------------------------------------
// Chat Store
// ---------------------------------------------------------------------------

interface ChatState {
  conversations: Conversation[];
  activeConversation: Conversation | null;
  messages: Message[];
  isSending: boolean;

  setConversations: (convs: Conversation[]) => void;
  setActiveConversation: (conv: Conversation | null) => void;
  setMessages: (msgs: Message[]) => void;
  addMessage: (msg: Message) => void;
  setIsSending: (v: boolean) => void;
  clearChat: () => void;
}

export const useChatStore = create<ChatState>((set) => ({
  conversations: [],
  activeConversation: null,
  messages: [],
  isSending: false,

  setConversations: (conversations) => set({ conversations }),
  setActiveConversation: (activeConversation) => set({ activeConversation }),
  setMessages: (messages) => set({ messages }),
  addMessage: (msg) => set((s) => ({ messages: [...s.messages, msg] })),
  setIsSending: (isSending) => set({ isSending }),
  clearChat: () => set({ messages: [], activeConversation: null }),
}));

// ---------------------------------------------------------------------------
// Emotion Store
// ---------------------------------------------------------------------------

interface EmotionState {
  currentEmotion: EmotionResult | null;
  emotionHistory: EmotionResult[];

  setCurrentEmotion: (e: EmotionResult | null) => void;
  pushEmotion: (e: EmotionResult) => void;
  clearEmotions: () => void;
}

export const useEmotionStore = create<EmotionState>((set) => ({
  currentEmotion: null,
  emotionHistory: [],

  setCurrentEmotion: (currentEmotion) => set({ currentEmotion }),
  pushEmotion: (e) =>
    set((s) => ({
      currentEmotion: e,
      emotionHistory: [...s.emotionHistory, e],
    })),
  clearEmotions: () => set({ currentEmotion: null, emotionHistory: [] }),
}));

// ---------------------------------------------------------------------------
// Avatar Store
// ---------------------------------------------------------------------------

interface AvatarState {
  avatarId: string | null;
  avatarUrl: string | null;
  thumbnailUrl: string | null;
  isGenerating: boolean;

  setAvatar: (id: string, url: string, thumbnail?: string) => void;
  setIsGenerating: (v: boolean) => void;
  clearAvatar: () => void;
  hydrate: () => Promise<void>;
}

export const useAvatarStore = create<AvatarState>((set) => ({
  avatarId: null,
  avatarUrl: null,
  thumbnailUrl: null,
  isGenerating: false,

  setAvatar: (id, url, thumbnail) => {
    set({ avatarId: id, avatarUrl: url, thumbnailUrl: thumbnail ?? null, isGenerating: false });
    secureSet("dt_avatar_id", id);
    secureSet("dt_avatar_url", url);
    if (thumbnail) secureSet("dt_avatar_thumbnail", thumbnail);
  },

  setIsGenerating: (isGenerating) => set({ isGenerating }),

  clearAvatar: () => {
    set({ avatarId: null, avatarUrl: null, thumbnailUrl: null, isGenerating: false });
    secureDelete("dt_avatar_id");
    secureDelete("dt_avatar_url");
    secureDelete("dt_avatar_thumbnail");
  },

  hydrate: async () => {
    try {
      const [id, url, thumbnail] = await Promise.all([
        secureGet("dt_avatar_id"),
        secureGet("dt_avatar_url"),
        secureGet("dt_avatar_thumbnail"),
      ]);
      if (id && url) {
        set({ avatarId: id, avatarUrl: url, thumbnailUrl: thumbnail });
      }
    } catch {
      // ignore hydration errors
    }
  },
}));

// ---------------------------------------------------------------------------
// Settings Store
// ---------------------------------------------------------------------------

interface SettingsState {
  selectedVoiceId: string | null;
  notificationsEnabled: boolean;
  personalityTraits: {
    friendliness: number;
    humor: number;
    empathy: number;
    formality: number;
  };

  setSelectedVoiceId: (id: string | null) => void;
  setNotificationsEnabled: (v: boolean) => void;
  setPersonalityTraits: (traits: Partial<SettingsState["personalityTraits"]>) => void;
  hydrate: () => Promise<void>;
}

export const useSettingsStore = create<SettingsState>((set, get) => ({
  selectedVoiceId: null,
  notificationsEnabled: true,
  personalityTraits: {
    friendliness: 0.7,
    humor: 0.5,
    empathy: 0.8,
    formality: 0.3,
  },

  setSelectedVoiceId: (id) => {
    set({ selectedVoiceId: id });
    if (id) secureSet("dt_voice_id", id);
    else secureDelete("dt_voice_id");
  },

  setNotificationsEnabled: (v) => {
    set({ notificationsEnabled: v });
    secureSet("dt_notifications", v ? "1" : "0");
  },

  setPersonalityTraits: (traits) => {
    const current = get().personalityTraits;
    const updated = { ...current, ...traits };
    set({ personalityTraits: updated });
    secureSet("dt_personality", JSON.stringify(updated));
  },

  hydrate: async () => {
    try {
      const [voiceId, notifications, personality] = await Promise.all([
        secureGet("dt_voice_id"),
        secureGet("dt_notifications"),
        secureGet("dt_personality"),
      ]);
      set({
        selectedVoiceId: voiceId,
        notificationsEnabled: notifications !== "0",
        personalityTraits: personality
          ? JSON.parse(personality)
          : { friendliness: 0.7, humor: 0.5, empathy: 0.8, formality: 0.3 },
      });
    } catch {
      // ignore hydration errors
    }
  },
}));
