import { useCallback, useRef, useState } from "react";
import { Platform } from "react-native";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { useAuthStore, useChatStore, useEmotionStore, useAvatarStore, useSettingsStore } from "./store";
import * as api from "./api";

// ---------------------------------------------------------------------------
// useAuth
// ---------------------------------------------------------------------------

export function useAuth() {
  const store = useAuthStore();
  const queryClient = useQueryClient();

  const loginMutation = useMutation({
    mutationFn: async ({
      email,
      password,
    }: {
      email: string;
      password: string;
    }) => {
      // Server expects Username, not email — pass email value as the username
      const res = await api.login(email, password);
      if (!res.success) throw new Error(res.message ?? "Login failed");
      return res.data;
    },
    onSuccess: (data) => {
      store.loginSuccess(
        {
          accessToken: data.accessToken,
          refreshToken: data.refreshToken,
          expiresIn: data.expiresIn,
        },
        data.user
      );
    },
  });

  const registerMutation = useMutation({
    mutationFn: async ({
      email,
      password,
      displayName,
    }: {
      email: string;
      password: string;
      displayName: string;
    }) => {
      // Map mobile form fields to server RegisterRequest shape
      const nameParts = displayName.trim().split(/\s+/);
      const firstName = nameParts[0] ?? "";
      const lastName = nameParts.slice(1).join(" ") || firstName;
      const res = await api.register({
        Username: email,
        Email: email,
        Password: password,
        FirstName: firstName,
        LastName: lastName,
      });
      if (!res.success) throw new Error(res.message ?? "Registration failed");
      return res.data;
    },
    onSuccess: (data) => {
      store.loginSuccess(
        {
          accessToken: data.accessToken,
          refreshToken: data.refreshToken,
          expiresIn: data.expiresIn,
        },
        data.user
      );
    },
  });

  const logout = useCallback(() => {
    store.logout();
    useChatStore.getState().clearChat();
    useEmotionStore.getState().clearEmotions();
    queryClient.clear();
  }, [store, queryClient]);

  return {
    user: store.user,
    isAuthenticated: store.isAuthenticated,
    isLoading: store.isLoading,
    login: loginMutation.mutateAsync,
    loginError: loginMutation.error,
    isLoggingIn: loginMutation.isPending,
    register: registerMutation.mutateAsync,
    registerError: registerMutation.error,
    isRegistering: registerMutation.isPending,
    logout,
  };
}

// ---------------------------------------------------------------------------
// useConversation
// ---------------------------------------------------------------------------

export function useConversation() {
  const chatStore = useChatStore();
  const emotionStore = useEmotionStore();
  const queryClient = useQueryClient();

  // No server-side list endpoint; conversations are tracked locally
  const conversationsQuery = {
    isLoading: false,
  };

  const startConversationMutation = useMutation({
    mutationFn: async (message: string) => {
      const res = await api.startConversation(message);
      if (!res.success) throw new Error(res.message ?? "Failed to start conversation");
      return res.data;
    },
    onSuccess: (data) => {
      // Map server ConversationStartResponse to local Conversation shape
      const conv: api.Conversation = {
        id: data.SessionId,
        userId: "",
        title: "New Conversation",
        createdAt: data.Timestamp,
        updatedAt: data.Timestamp,
        messageCount: 1,
        lastEmotion: data.EmotionalTone,
      };
      chatStore.setActiveConversation(conv);
      chatStore.setMessages([]);
    },
  });

  const loadHistory = useCallback(
    async (conversationId: string) => {
      const res = await api.getConversationHistory(conversationId);
      if (res.success) {
        const history = res.data;
        // Map server ConversationMessage[] to local Message[] for the chat store
        const mapped: api.Message[] = [];
        for (const m of history.Messages) {
          mapped.push({
            id: m.Id,
            conversationId,
            role: "user",
            content: m.Content,
            timestamp: m.Timestamp,
          });
          if (m.Response) {
            mapped.push({
              id: `${m.Id}-resp`,
              conversationId,
              role: "assistant",
              content: m.Response,
              timestamp: m.Timestamp,
            });
          }
        }
        chatStore.setMessages(mapped);
      }
    },
    [chatStore, emotionStore]
  );

  const sendMessageMutation = useMutation({
    mutationFn: async ({
      content,
    }: {
      content: string;
    }) => {
      let conversationId = chatStore.activeConversation?.id;

      if (!conversationId) {
        // Start a new conversation with the first message
        const startRes = await api.startConversation(content);
        if (!startRes.success)
          throw new Error("Failed to start conversation");
        const conv: api.Conversation = {
          id: startRes.data.SessionId,
          userId: "",
          title: "New Conversation",
          createdAt: startRes.data.Timestamp,
          updatedAt: startRes.data.Timestamp,
          messageCount: 1,
          lastEmotion: startRes.data.EmotionalTone,
        };
        chatStore.setActiveConversation(conv);
        conversationId = startRes.data.SessionId;

        // The start endpoint already returns a greeting response
        chatStore.setIsSending(false);
        return {
          Response: startRes.data.Response,
          DetectedEmotion: startRes.data.EmotionalTone,
          AIEmotionalTone: startRes.data.EmotionalTone,
          ResponseTime: startRes.data.Timestamp,
          ConversationId: conversationId,
          _initialMessage: content,
        } as api.ConversationMessageResponse & { _initialMessage?: string };
      }

      chatStore.setIsSending(true);

      // Optimistically add the user message
      const tempUserMsg: api.Message = {
        id: `temp-${Date.now()}`,
        conversationId: conversationId!,
        role: "user",
        content,
        timestamp: new Date().toISOString(),
      };
      chatStore.addMessage(tempUserMsg);

      const res = await api.sendMessage(conversationId!, content);

      if (!res.success) throw new Error(res.message ?? "Failed to send message");
      return { ...res.data, _initialMessage: undefined } as api.ConversationMessageResponse & { _initialMessage?: string };
    },
    onSuccess: (data) => {
      // Replace temp user message and add user + assistant messages
      const messages = chatStore.messages.filter(
        (m) => !m.id.startsWith("temp-")
      );

      const convId = data.ConversationId;
      const now = data.ResponseTime;

      // If this was the initial message via startConversation, add both user and assistant
      if (data._initialMessage) {
        messages.push({
          id: `user-${Date.now()}`,
          conversationId: convId,
          role: "user",
          content: data._initialMessage,
          timestamp: now,
        });
      }

      messages.push({
        id: `assistant-${Date.now()}`,
        conversationId: convId,
        role: "assistant",
        content: data.Response,
        timestamp: now,
      });

      chatStore.setMessages(messages);
      chatStore.setIsSending(false);
    },
    onError: () => {
      // Remove temp messages on failure
      chatStore.setMessages(
        chatStore.messages.filter((m) => !m.id.startsWith("temp-"))
      );
      chatStore.setIsSending(false);
    },
  });

  return {
    conversations: chatStore.conversations,
    activeConversation: chatStore.activeConversation,
    messages: chatStore.messages,
    isSending: chatStore.isSending,
    isLoadingConversations: conversationsQuery.isLoading,
    startConversation: startConversationMutation.mutateAsync,
    loadHistory,
    sendMessage: sendMessageMutation.mutateAsync,
    setActiveConversation: chatStore.setActiveConversation,
  };
}

// ---------------------------------------------------------------------------
// useEmotion
// ---------------------------------------------------------------------------

export function useEmotion() {
  const store = useEmotionStore();

  const insightsQuery = useQuery({
    queryKey: ["emotionInsights"],
    queryFn: async () => {
      const res = await api.getEmotionInsights("week");
      if (!res.success) throw new Error(res.message ?? "Failed to load insights");
      return res.data;
    },
    enabled: useAuthStore.getState().isAuthenticated,
    staleTime: 5 * 60 * 1000, // 5 minutes
  });

  return {
    currentEmotion: store.currentEmotion,
    emotionHistory: store.emotionHistory,
    insights: insightsQuery.data ?? null,
    isLoadingInsights: insightsQuery.isLoading,
    refetchInsights: insightsQuery.refetch,
  };
}

// ---------------------------------------------------------------------------
// Emotion color helper
// ---------------------------------------------------------------------------

export const EMOTION_COLORS: Record<string, string> = {
  joy: "#FFD166",
  happiness: "#FFD166",
  calm: "#A8D8B9",
  sadness: "#7BA7C9",
  anger: "#E07A7A",
  fear: "#8C8CBF",
  surprise: "#C5A3E0",
  love: "#F5A0B8",
  disgust: "#9AB87A",
  neutral: "#C7B8A5",
};

export function getEmotionColor(emotion: string | undefined): string {
  if (!emotion) return EMOTION_COLORS.neutral;
  return EMOTION_COLORS[emotion.toLowerCase()] ?? EMOTION_COLORS.neutral;
}

export function getEmotionBgTint(emotion: string | undefined): string {
  const base = getEmotionColor(emotion);
  // Return a very light tint for backgrounds
  return base + "18"; // ~10% opacity via hex alpha
}

// ---------------------------------------------------------------------------
// useVoice
// ---------------------------------------------------------------------------

export function useVoice() {
  const queryClient = useQueryClient();
  const settingsStore = useSettingsStore();

  const voicesQuery = useQuery({
    queryKey: ["voices"],
    queryFn: async () => {
      const res = await api.listVoices();
      if (!res.success) throw new Error(res.message ?? "Failed to load voices");
      return res.data;
    },
    enabled: useAuthStore.getState().isAuthenticated,
  });

  const cloneMutation = useMutation({
    mutationFn: async (formData: FormData) => {
      const res = await api.cloneVoice(formData);
      if (!res.success) throw new Error(res.message ?? "Voice clone failed");
      return res.data;
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["voices"] });
    },
  });

  return {
    voices: voicesQuery.data ?? [],
    isLoadingVoices: voicesQuery.isLoading,
    selectedVoiceId: settingsStore.selectedVoiceId,
    selectVoice: settingsStore.setSelectedVoiceId,
    cloneVoice: cloneMutation.mutateAsync,
    isCloning: cloneMutation.isPending,
    cloneError: cloneMutation.error,
  };
}

// ---------------------------------------------------------------------------
// useAvatar
// ---------------------------------------------------------------------------

export function useAvatar() {
  const avatarStore = useAvatarStore();

  const generateMutation = useMutation({
    mutationFn: async (formData: FormData) => {
      avatarStore.setIsGenerating(true);
      const res = await api.generateAvatar(formData);
      if (!res.success) throw new Error(res.message ?? "Avatar generation failed");
      return res.data;
    },
    onError: () => {
      avatarStore.setIsGenerating(false);
    },
  });

  const statusQuery = useQuery({
    queryKey: ["avatarStatus", avatarStore.avatarId],
    queryFn: async () => {
      if (!avatarStore.avatarId) return null;
      const res = await api.getAvatarStatus(avatarStore.avatarId);
      if (!res.success) throw new Error(res.message ?? "Failed to check status");
      if (res.data.status === "completed" && res.data.avatarUrl) {
        avatarStore.setAvatar(
          avatarStore.avatarId!,
          res.data.avatarUrl,
          res.data.thumbnailUrl
        );
      } else if (res.data.status === "failed") {
        avatarStore.setIsGenerating(false);
      }
      return res.data;
    },
    enabled: avatarStore.isGenerating && !!avatarStore.avatarId,
    refetchInterval: (query) => {
      const status = query.state.data?.status;
      if (status === "completed" || status === "failed") return false;
      return 3000; // poll every 3s
    },
  });

  return {
    avatarId: avatarStore.avatarId,
    avatarUrl: avatarStore.avatarUrl,
    thumbnailUrl: avatarStore.thumbnailUrl,
    isGenerating: avatarStore.isGenerating,
    avatarStatus: statusQuery.data,
    generateAvatar: async (formData: FormData) => {
      const result = await generateMutation.mutateAsync(formData);
      avatarStore.setAvatar(result.id, "", undefined);
      avatarStore.setIsGenerating(true);
      return result;
    },
    deleteAvatar: async () => {
      if (avatarStore.avatarId) {
        await api.deleteAvatar(avatarStore.avatarId);
      }
      avatarStore.clearAvatar();
    },
    generateError: generateMutation.error,
  };
}

// ---------------------------------------------------------------------------
// useSubscription
// ---------------------------------------------------------------------------

export function useSubscription() {
  const queryClient = useQueryClient();

  const tiersQuery = useQuery({
    queryKey: ["subscriptionTiers"],
    queryFn: async () => {
      const res = await api.getSubscriptionTiers();
      if (!res.success) throw new Error(res.message ?? "Failed to load tiers");
      return res.data;
    },
    staleTime: 60 * 60 * 1000, // 1 hour
  });

  const currentQuery = useQuery({
    queryKey: ["subscription"],
    queryFn: async () => {
      const res = await api.getSubscription();
      if (!res.success) throw new Error(res.message ?? "Failed to load subscription");
      return res.data;
    },
    enabled: useAuthStore.getState().isAuthenticated,
  });

  const checkoutMutation = useMutation({
    mutationFn: async ({
      tier,
      platform,
    }: {
      tier: api.SubscriptionTier;
      platform: "web" | "mobile";
    }) => {
      const res = await api.createCheckoutSession(tier, platform);
      if (!res.success) throw new Error(res.message ?? "Checkout failed");
      return res.data;
    },
  });

  const cancelMutation = useMutation({
    mutationFn: async () => {
      const res = await api.cancelSubscription();
      if (!res.success) throw new Error(res.message ?? "Cancel failed");
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["subscription"] });
    },
  });

  return {
    tiers: tiersQuery.data ?? [],
    isLoadingTiers: tiersQuery.isLoading,
    subscription: currentQuery.data ?? null,
    isLoadingSubscription: currentQuery.isLoading,
    createCheckout: checkoutMutation.mutateAsync,
    isCheckingOut: checkoutMutation.isPending,
    cancelSubscription: cancelMutation.mutateAsync,
    isCanceling: cancelMutation.isPending,
    refetch: () => queryClient.invalidateQueries({ queryKey: ["subscription"] }),
  };
}

// ---------------------------------------------------------------------------
// useCheckIn
// ---------------------------------------------------------------------------

export function useCheckIn() {
  const queryClient = useQueryClient();

  const pendingQuery = useQuery({
    queryKey: ["checkIns"],
    queryFn: async () => {
      const res = await api.getPendingCheckIns();
      if (!res.success) throw new Error(res.message ?? "Failed to load check-ins");
      return res.data;
    },
    enabled: useAuthStore.getState().isAuthenticated,
    refetchInterval: 5 * 60 * 1000, // poll every 5 minutes
  });

  const respondMutation = useMutation({
    mutationFn: async ({ id, response }: { id: string; response: string }) => {
      const res = await api.respondToCheckIn(id, response);
      if (!res.success) throw new Error(res.message ?? "Failed to respond");
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["checkIns"] });
    },
  });

  return {
    pendingCheckIns: pendingQuery.data ?? [],
    isLoading: pendingQuery.isLoading,
    respond: respondMutation.mutateAsync,
    isResponding: respondMutation.isPending,
    refetch: pendingQuery.refetch,
  };
}

// ---------------------------------------------------------------------------
// useTTS
// ---------------------------------------------------------------------------

export function useTTS() {
  const [isPlaying, setIsPlaying] = useState(false);
  const [playingMessageId, setPlayingMessageId] = useState<string | null>(null);
  const soundRef = useRef<any>(null);
  const cacheRef = useRef<Map<string, string>>(new Map());
  const settingsStore = useSettingsStore();

  const play = useCallback(
    async (messageId: string, text: string) => {
      try {
        // Stop current playback
        if (soundRef.current) {
          await soundRef.current.unloadAsync();
          soundRef.current = null;
        }

        setIsPlaying(true);
        setPlayingMessageId(messageId);

        const blob = await api.textToSpeech({
          text,
          voiceId: settingsStore.selectedVoiceId ?? undefined,
        });

        let uri: string;
        const cached = cacheRef.current.get(messageId);
        if (cached) {
          uri = cached;
        } else if (Platform.OS === "web") {
          uri = URL.createObjectURL(blob);
          cacheRef.current.set(messageId, uri);
        } else {
          // Write to cache directory on native
          const FileSystem = await import("expo-file-system");
          const filePath = `${FileSystem.cacheDirectory}tts_${messageId}.mp3`;
          const reader = new FileReader();
          const base64 = await new Promise<string>((resolve) => {
            reader.onloadend = () => {
              const result = reader.result as string;
              resolve(result.split(",")[1]);
            };
            reader.readAsDataURL(blob);
          });
          await FileSystem.writeAsStringAsync(filePath, base64, {
            encoding: FileSystem.EncodingType.Base64,
          });
          uri = filePath;
          cacheRef.current.set(messageId, uri);
        }

        const { Audio } = await import("expo-av");
        const { sound } = await Audio.Sound.createAsync({ uri });
        soundRef.current = sound;

        sound.setOnPlaybackStatusUpdate((status: any) => {
          if (status.didJustFinish) {
            setIsPlaying(false);
            setPlayingMessageId(null);
            sound.unloadAsync();
            soundRef.current = null;
          }
        });

        await sound.playAsync();
      } catch (err) {
        console.error("TTS playback failed:", err);
        setIsPlaying(false);
        setPlayingMessageId(null);
      }
    },
    [settingsStore.selectedVoiceId]
  );

  const stop = useCallback(async () => {
    if (soundRef.current) {
      await soundRef.current.stopAsync();
      await soundRef.current.unloadAsync();
      soundRef.current = null;
    }
    setIsPlaying(false);
    setPlayingMessageId(null);
  }, []);

  return {
    isPlaying,
    playingMessageId,
    play,
    stop,
  };
}
