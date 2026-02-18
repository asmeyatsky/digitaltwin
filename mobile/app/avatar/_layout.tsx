import { Stack } from "expo-router";

export default function AvatarLayout() {
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
      <Stack.Screen
        name="setup"
        options={{ title: "Avatar Setup" }}
      />
    </Stack>
  );
}
