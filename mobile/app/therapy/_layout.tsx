import { Stack } from "expo-router";

export default function TherapyLayout() {
  return (
    <Stack
      screenOptions={{
        headerShown: true,
        headerStyle: { backgroundColor: "#FDF8F3" },
        headerTintColor: "#3D2E22",
        headerBackTitle: "Back",
        contentStyle: { backgroundColor: "#FDF8F3" },
      }}
    >
      <Stack.Screen name="index" options={{ title: "Therapy & Screening" }} />
      <Stack.Screen name="therapists" options={{ title: "Find a Therapist" }} />
      <Stack.Screen name="screening" options={{ title: "Clinical Screening" }} />
    </Stack>
  );
}
