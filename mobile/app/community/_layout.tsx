import { Stack } from "expo-router";

export default function CommunityLayout() {
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
      <Stack.Screen name="index" options={{ title: "Community" }} />
      <Stack.Screen name="[groupId]" options={{ title: "Group" }} />
      <Stack.Screen name="post/[postId]" options={{ title: "Post" }} />
      <Stack.Screen name="create-post" options={{ title: "New Post" }} />
    </Stack>
  );
}
