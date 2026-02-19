import React, { useState } from "react";
import { View, Text, Pressable, ScrollView, Alert, Platform } from "react-native";
import { SafeAreaView } from "react-native-safe-area-context";
import { useRouter } from "expo-router";
import { Camera } from "expo-camera";
import { Audio } from "expo-av";
import * as Notifications from "expo-notifications";

interface PermissionCard {
  id: string;
  icon: string;
  title: string;
  description: string;
}

const permissionCards: PermissionCard[] = [
  {
    id: "camera",
    icon: "\uD83D\uDCF7",
    title: "Camera",
    description: "Detect emotions from your facial expressions",
  },
  {
    id: "microphone",
    icon: "\uD83C\uDF99\uFE0F",
    title: "Microphone",
    description: "Talk to your companion with your voice",
  },
  {
    id: "notifications",
    icon: "\uD83D\uDD14",
    title: "Notifications",
    description: "Get proactive check-ins when you need them",
  },
  {
    id: "health",
    icon: "\u2764\uFE0F",
    title: "Health Data",
    description: "Integrate biometric data for deeper understanding",
  },
];

export default function PermissionsScreen() {
  const router = useRouter();
  const [granted, setGranted] = useState<Record<string, boolean>>({});

  const requestPermission = async (id: string) => {
    try {
      let result = false;

      switch (id) {
        case "camera": {
          const { status } = await Camera.requestCameraPermissionsAsync();
          result = status === "granted";
          break;
        }
        case "microphone": {
          const { status } = await Audio.requestPermissionsAsync();
          result = status === "granted";
          break;
        }
        case "notifications": {
          const { status } = await Notifications.requestPermissionsAsync();
          result = status === "granted";
          break;
        }
        case "health": {
          // Health data requires HealthKit (iOS) / Health Connect (Android)
          // Show informational alert for now
          if (Platform.OS === "web") {
            result = false;
          } else {
            Alert.alert(
              "Health Data",
              "Health data integration will be available in a future update.",
              [{ text: "OK" }]
            );
            result = false;
          }
          break;
        }
      }

      setGranted((prev) => ({ ...prev, [id]: result }));
    } catch (error) {
      console.error(`Failed to request ${id} permission:`, error);
    }
  };

  const handleContinue = () => {
    router.push("/onboarding/complete");
  };

  return (
    <SafeAreaView className="flex-1 bg-companion-bg">
      <ScrollView
        contentContainerStyle={{ paddingHorizontal: 24, paddingTop: 48, paddingBottom: 120 }}
        showsVerticalScrollIndicator={false}
      >
        {/* Header */}
        <Text className="text-3xl font-bold text-warmgray-800 text-center mb-2">
          Enable Permissions
        </Text>
        <Text className="text-base text-warmgray-500 text-center mb-10">
          These are optional but help your companion understand you better
        </Text>

        {/* Permission Cards */}
        <View className="gap-4">
          {permissionCards.map((card) => {
            const isGranted = granted[card.id] === true;
            return (
              <View
                key={card.id}
                className="bg-white border border-warmgray-100 rounded-2xl px-5 py-5 flex-row items-center"
              >
                {/* Icon */}
                <View className="w-12 h-12 rounded-full bg-primary-50 items-center justify-center mr-4">
                  <Text style={{ fontSize: 24 }}>{card.icon}</Text>
                </View>

                {/* Text */}
                <View className="flex-1 mr-3">
                  <Text className="text-base font-semibold text-warmgray-800">
                    {card.title}
                  </Text>
                  <Text className="text-sm text-warmgray-500 mt-0.5 leading-5">
                    {card.description}
                  </Text>
                </View>

                {/* Enable / Enabled button */}
                <Pressable
                  onPress={() => requestPermission(card.id)}
                  disabled={isGranted}
                  className={`rounded-full px-4 py-2 ${
                    isGranted ? "bg-green-50" : "bg-primary-500"
                  }`}
                  style={({ pressed }) => ({
                    opacity: pressed && !isGranted ? 0.85 : 1,
                  })}
                >
                  <Text
                    className={`text-sm font-semibold ${
                      isGranted ? "text-green-600" : "text-white"
                    }`}
                  >
                    {isGranted ? "Enabled" : "Enable"}
                  </Text>
                </Pressable>
              </View>
            );
          })}
        </View>
      </ScrollView>

      {/* Continue Button — always enabled */}
      <View className="absolute bottom-0 left-0 right-0 px-6 pb-8 pt-4 bg-companion-bg">
        <Pressable
          onPress={handleContinue}
          className="w-full bg-primary-500 rounded-2xl py-4 items-center"
          style={({ pressed }) => ({ opacity: pressed ? 0.85 : 1 })}
        >
          <Text className="text-white text-base font-semibold">
            Continue
          </Text>
        </Pressable>
      </View>
    </SafeAreaView>
  );
}
