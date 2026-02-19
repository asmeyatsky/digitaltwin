import { Stack } from "expo-router";

export default function CreativeLayout() {
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
      <Stack.Screen name="index" options={{ title: "Creative Studio" }} />
      <Stack.Screen name="editor" options={{ title: "Write" }} />
      <Stack.Screen name="prompts" options={{ title: "Creative Prompts" }} />
    </Stack>
  );
}
