import React from "react";
import { Tabs } from "expo-router";
import { View, Text, Platform, useWindowDimensions } from "react-native";

// Simple SVG-free icon components for cross-platform compatibility
function ChatIcon({ color, focused }: { color: string; focused: boolean }) {
  return (
    <View
      className={`items-center justify-center ${
        focused ? "opacity-100" : "opacity-60"
      }`}
    >
      <View
        style={{
          width: 24,
          height: 22,
          borderRadius: 12,
          borderWidth: 2,
          borderColor: color,
          backgroundColor: focused ? color + "20" : "transparent",
        }}
      />
      <View
        style={{
          width: 0,
          height: 0,
          borderLeftWidth: 6,
          borderRightWidth: 0,
          borderTopWidth: 6,
          borderLeftColor: "transparent",
          borderRightColor: "transparent",
          borderTopColor: color,
          alignSelf: "flex-start",
          marginLeft: 4,
        }}
      />
    </View>
  );
}

function InsightsIcon({ color, focused }: { color: string; focused: boolean }) {
  return (
    <View
      className={`flex-row items-end gap-0.5 ${
        focused ? "opacity-100" : "opacity-60"
      }`}
      style={{ height: 24, width: 24 }}
    >
      <View
        style={{
          width: 5,
          height: 10,
          backgroundColor: color,
          borderRadius: 2,
        }}
      />
      <View
        style={{
          width: 5,
          height: 18,
          backgroundColor: color,
          borderRadius: 2,
        }}
      />
      <View
        style={{
          width: 5,
          height: 14,
          backgroundColor: color,
          borderRadius: 2,
        }}
      />
      <View
        style={{
          width: 5,
          height: 22,
          backgroundColor: color,
          borderRadius: 2,
        }}
      />
    </View>
  );
}

function ProfileIcon({ color, focused }: { color: string; focused: boolean }) {
  return (
    <View
      className={`items-center ${focused ? "opacity-100" : "opacity-60"}`}
    >
      <View
        style={{
          width: 12,
          height: 12,
          borderRadius: 6,
          borderWidth: 2,
          borderColor: color,
          backgroundColor: focused ? color + "20" : "transparent",
        }}
      />
      <View
        style={{
          width: 20,
          height: 10,
          borderTopLeftRadius: 10,
          borderTopRightRadius: 10,
          borderWidth: 2,
          borderBottomWidth: 0,
          borderColor: color,
          backgroundColor: focused ? color + "20" : "transparent",
          marginTop: 2,
        }}
      />
    </View>
  );
}

export default function TabLayout() {
  const { width } = useWindowDimensions();
  const isWideScreen = width >= 768;

  return (
    <Tabs
      screenOptions={{
        headerShown: false,
        tabBarActiveTintColor: "#FF8B47",
        tabBarInactiveTintColor: "#A89885",
        tabBarPosition: isWideScreen ? "left" : "bottom",
        tabBarStyle: isWideScreen
          ? {
              backgroundColor: "#FFFFFF",
              borderRightColor: "#EBE1D6",
              borderRightWidth: 1,
              width: 200,
              paddingTop: 48,
              elevation: 0,
              shadowOpacity: 0,
            }
          : {
              backgroundColor: "#FFFFFF",
              borderTopColor: "#EBE1D6",
              borderTopWidth: 1,
              height: Platform.OS === "ios" ? 88 : 64,
              paddingTop: 8,
              paddingBottom: Platform.OS === "ios" ? 28 : 8,
              elevation: 0,
              shadowOpacity: 0,
            },
        tabBarLabelStyle: isWideScreen
          ? { fontSize: 14, fontWeight: "600", marginLeft: 8 }
          : { fontSize: 12, fontWeight: "600", marginTop: 4 },
      }}
    >
      <Tabs.Screen
        name="chat"
        options={{
          title: "Chat",
          tabBarIcon: ({ color, focused }) => (
            <ChatIcon color={color} focused={focused} />
          ),
        }}
      />
      <Tabs.Screen
        name="insights"
        options={{
          title: "Insights",
          tabBarIcon: ({ color, focused }) => (
            <InsightsIcon color={color} focused={focused} />
          ),
        }}
      />
      <Tabs.Screen
        name="profile"
        options={{
          title: "Profile",
          tabBarIcon: ({ color, focused }) => (
            <ProfileIcon color={color} focused={focused} />
          ),
        }}
      />
    </Tabs>
  );
}
