import "../global.css";
import React, { useEffect, useState } from "react";
import { Platform } from "react-native";
import { Stack, useRouter, useSegments } from "expo-router";
import { StatusBar } from "expo-status-bar";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { SafeAreaProvider } from "react-native-safe-area-context";
import { GestureHandlerRootView } from "react-native-gesture-handler";
import AsyncStorage from "@react-native-async-storage/async-storage";
import { useAuthStore } from "@/lib/store";
import {
  registerForPushNotifications,
  setupNotificationListeners,
} from "@/lib/notifications";

const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      retry: 2,
      staleTime: 30_000,
      refetchOnWindowFocus: false,
    },
  },
});

function AuthGate({ children }: { children: React.ReactNode }) {
  const { isAuthenticated, isLoading, hydrate } = useAuthStore();
  const segments = useSegments();
  const router = useRouter();
  const [hasCompletedOnboarding, setHasCompletedOnboarding] = useState<
    boolean | null
  >(null);

  useEffect(() => {
    hydrate();
  }, [hydrate]);

  // Check onboarding status on mount
  useEffect(() => {
    (async () => {
      try {
        const value = await AsyncStorage.getItem("hasCompletedOnboarding");
        setHasCompletedOnboarding(value === "true");
      } catch {
        setHasCompletedOnboarding(false);
      }
    })();
  }, []);

  useEffect(() => {
    if (isLoading || hasCompletedOnboarding === null) return;

    const inAuthGroup = segments[0] === "(auth)";
    const inOnboarding = segments[0] === "onboarding";

    // First-time user: send to onboarding
    if (!hasCompletedOnboarding && !inOnboarding) {
      router.replace("/onboarding/welcome");
      return;
    }

    // Onboarding complete but still on an onboarding screen — skip
    if (hasCompletedOnboarding && inOnboarding) {
      if (!isAuthenticated) {
        router.replace("/(auth)/login");
      } else {
        router.replace("/(tabs)/chat");
      }
      return;
    }

    // Normal auth gating (only after onboarding is done)
    if (hasCompletedOnboarding) {
      if (!isAuthenticated && !inAuthGroup) {
        router.replace("/(auth)/login");
      } else if (isAuthenticated && inAuthGroup) {
        router.replace("/(tabs)/chat");
      }
    }
  }, [isAuthenticated, isLoading, hasCompletedOnboarding, segments, router]);

  return <>{children}</>;
}

// Conditionally import StripeProvider on native platforms
const StripeProvider =
  Platform.OS !== "web"
    ? require("@stripe/stripe-react-native").StripeProvider
    : ({ children }: { children: React.ReactNode }) => <>{children}</>;

const STRIPE_PUBLISHABLE_KEY =
  process.env.EXPO_PUBLIC_STRIPE_PUBLISHABLE_KEY ?? "";

export default function RootLayout() {
  // Register for push notifications on app mount
  useEffect(() => {
    registerForPushNotifications();
    const cleanup = setupNotificationListeners();
    return cleanup;
  }, []);

  return (
    <GestureHandlerRootView style={{ flex: 1 }}>
      <SafeAreaProvider>
        <QueryClientProvider client={queryClient}>
          <StripeProvider
            publishableKey={STRIPE_PUBLISHABLE_KEY}
            merchantIdentifier="merchant.com.digitaltwin.companion"
          >
          <AuthGate>
            <Stack
              screenOptions={{
                headerShown: false,
                contentStyle: { backgroundColor: "#FDF8F3" },
                animation: "slide_from_right",
              }}
            >
              <Stack.Screen name="onboarding" options={{ headerShown: false }} />
              <Stack.Screen name="(auth)" options={{ headerShown: false }} />
              <Stack.Screen name="(tabs)" options={{ headerShown: false }} />
              <Stack.Screen
                name="avatar"
                options={{
                  headerShown: false,
                  presentation: "modal",
                  animation: "slide_from_bottom",
                }}
              />
              <Stack.Screen
                name="camera"
                options={{
                  headerShown: false,
                  presentation: "fullScreenModal",
                  animation: "slide_from_bottom",
                }}
              />
              <Stack.Screen
                name="settings"
                options={{
                  headerShown: false,
                  animation: "slide_from_right",
                }}
              />
            </Stack>
          </AuthGate>
          </StripeProvider>
          <StatusBar style="dark" />
        </QueryClientProvider>
      </SafeAreaProvider>
    </GestureHandlerRootView>
  );
}
