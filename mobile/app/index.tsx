import { Redirect } from "expo-router";
import { useAuthStore } from "@/lib/store";

export default function Index() {
  const isAuthenticated = useAuthStore((s) => s.isAuthenticated);

  if (isAuthenticated) {
    return <Redirect href="/(tabs)/chat" />;
  }

  return <Redirect href="/(auth)/login" />;
}
