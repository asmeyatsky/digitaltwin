import { Stack } from "expo-router";

export default function CameraLayout() {
  return (
    <Stack
      screenOptions={{
        headerShown: false,
        contentStyle: { backgroundColor: "#000" },
      }}
    >
      <Stack.Screen name="emotion" />
    </Stack>
  );
}
