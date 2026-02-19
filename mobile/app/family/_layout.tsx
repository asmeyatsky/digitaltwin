import { Stack } from "expo-router";

export default function FamilyLayout() {
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
      <Stack.Screen name="index" options={{ title: "My Family" }} />
      <Stack.Screen name="invite" options={{ title: "Invite Member" }} />
      <Stack.Screen name="insights" options={{ title: "Family Insights" }} />
    </Stack>
  );
}
