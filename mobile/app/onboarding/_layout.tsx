import React from "react";
import { Stack } from "expo-router";

export default function OnboardingLayout() {
  return (
    <Stack
      screenOptions={{
        headerShown: false,
        contentStyle: { backgroundColor: "#FDF8F3" },
        animation: "slide_from_right",
      }}
    >
      <Stack.Screen name="welcome" />
      <Stack.Screen name="mood-check" />
      <Stack.Screen name="personalize" />
      <Stack.Screen name="permissions" />
      <Stack.Screen name="complete" />
    </Stack>
  );
}
