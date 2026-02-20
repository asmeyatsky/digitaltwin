import {
  useAuthStore,
  useChatStore,
  useEmotionStore,
  useSettingsStore,
} from "@/lib/store";

beforeEach(() => {
  // Reset all stores to initial state
  useAuthStore.setState({
    token: null,
    refreshToken: null,
    user: null,
    isAuthenticated: false,
    isLoading: true,
  });
  useChatStore.setState({
    conversations: [],
    activeConversation: null,
    messages: [],
    isSending: false,
  });
  useEmotionStore.setState({
    currentEmotion: null,
    emotionHistory: [],
  });
  useSettingsStore.setState({
    selectedVoiceId: null,
    notificationsEnabled: true,
    personalityTraits: {
      friendliness: 0.7,
      humor: 0.5,
      empathy: 0.8,
      formality: 0.3,
    },
  });
});

describe("AuthStore", () => {
  test("initial state", () => {
    const state = useAuthStore.getState();
    expect(state.token).toBeNull();
    expect(state.refreshToken).toBeNull();
    expect(state.user).toBeNull();
    expect(state.isAuthenticated).toBe(false);
    expect(state.isLoading).toBe(true);
  });

  test("setTokens", () => {
    useAuthStore.getState().setTokens("access-123", "refresh-456");
    const state = useAuthStore.getState();
    expect(state.token).toBe("access-123");
    expect(state.refreshToken).toBe("refresh-456");
    expect(state.isAuthenticated).toBe(true);
  });

  test("loginSuccess", () => {
    const user = { id: "u1", username: "alice", roles: ["user"] };
    useAuthStore.getState().loginSuccess(
      { accessToken: "at", refreshToken: "rt", expiresIn: 3600 },
      user
    );
    const state = useAuthStore.getState();
    expect(state.token).toBe("at");
    expect(state.refreshToken).toBe("rt");
    expect(state.user).toEqual(user);
    expect(state.isAuthenticated).toBe(true);
    expect(state.isLoading).toBe(false);
  });

  test("logout", () => {
    useAuthStore.getState().setTokens("t", "r");
    useAuthStore.getState().logout();
    const state = useAuthStore.getState();
    expect(state.token).toBeNull();
    expect(state.isAuthenticated).toBe(false);
  });

  test("hydrate with no tokens", async () => {
    await useAuthStore.getState().hydrate();
    const state = useAuthStore.getState();
    expect(state.isLoading).toBe(false);
    expect(state.isAuthenticated).toBe(false);
  });
});

describe("ChatStore", () => {
  test("initial state", () => {
    const state = useChatStore.getState();
    expect(state.messages).toEqual([]);
    expect(state.activeConversation).toBeNull();
    expect(state.isSending).toBe(false);
  });

  test("addMessage", () => {
    const msg = {
      id: "m1",
      conversationId: "c1",
      role: "user" as const,
      content: "hello",
      timestamp: "2024-01-01",
    };
    useChatStore.getState().addMessage(msg);
    expect(useChatStore.getState().messages).toHaveLength(1);
    expect(useChatStore.getState().messages[0].content).toBe("hello");
  });

  test("clearChat", () => {
    useChatStore.getState().addMessage({
      id: "m1",
      conversationId: "c1",
      role: "user",
      content: "hello",
      timestamp: "2024-01-01",
    });
    useChatStore.getState().clearChat();
    expect(useChatStore.getState().messages).toEqual([]);
    expect(useChatStore.getState().activeConversation).toBeNull();
  });

  test("setIsSending", () => {
    useChatStore.getState().setIsSending(true);
    expect(useChatStore.getState().isSending).toBe(true);
  });
});

describe("EmotionStore", () => {
  test("pushEmotion", () => {
    const emotion = { primary: "joy", confidence: 0.9, valence: 0.8, arousal: 0.6 };
    useEmotionStore.getState().pushEmotion(emotion);
    const state = useEmotionStore.getState();
    expect(state.currentEmotion).toEqual(emotion);
    expect(state.emotionHistory).toHaveLength(1);
  });

  test("clearEmotions", () => {
    useEmotionStore.getState().pushEmotion({
      primary: "joy",
      confidence: 0.9,
      valence: 0.8,
      arousal: 0.6,
    });
    useEmotionStore.getState().clearEmotions();
    expect(useEmotionStore.getState().currentEmotion).toBeNull();
    expect(useEmotionStore.getState().emotionHistory).toEqual([]);
  });
});

describe("SettingsStore", () => {
  test("default personality traits", () => {
    const traits = useSettingsStore.getState().personalityTraits;
    expect(traits.friendliness).toBe(0.7);
    expect(traits.humor).toBe(0.5);
    expect(traits.empathy).toBe(0.8);
    expect(traits.formality).toBe(0.3);
  });

  test("setPersonalityTraits merges", () => {
    useSettingsStore.getState().setPersonalityTraits({ humor: 0.9 });
    const traits = useSettingsStore.getState().personalityTraits;
    expect(traits.humor).toBe(0.9);
    // Other values unchanged
    expect(traits.friendliness).toBe(0.7);
  });

  test("setNotificationsEnabled", () => {
    useSettingsStore.getState().setNotificationsEnabled(false);
    expect(useSettingsStore.getState().notificationsEnabled).toBe(false);
  });
});
