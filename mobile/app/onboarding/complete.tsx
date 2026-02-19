import React, { useEffect, useRef } from "react";
import { View, Text, Pressable, Animated } from "react-native";
import { SafeAreaView } from "react-native-safe-area-context";
import { useRouter } from "expo-router";
import AsyncStorage from "@react-native-async-storage/async-storage";

export default function CompleteScreen() {
  const router = useRouter();
  const scaleAnim = useRef(new Animated.Value(0)).current;
  const opacityAnim = useRef(new Animated.Value(0)).current;

  useEffect(() => {
    // Animate the checkmark in
    Animated.sequence([
      Animated.spring(scaleAnim, {
        toValue: 1,
        friction: 4,
        tension: 60,
        useNativeDriver: true,
      }),
      Animated.timing(opacityAnim, {
        toValue: 1,
        duration: 400,
        useNativeDriver: true,
      }),
    ]).start();
  }, [scaleAnim, opacityAnim]);

  const handleStartChatting = async () => {
    try {
      await AsyncStorage.setItem("hasCompletedOnboarding", "true");
    } catch (error) {
      console.error("Failed to save onboarding state:", error);
    }
    router.replace("/(tabs)/chat");
  };

  return (
    <SafeAreaView className="flex-1 bg-companion-bg">
      <View className="flex-1 items-center justify-center px-8">
        {/* Animated checkmark */}
        <Animated.View
          style={{ transform: [{ scale: scaleAnim }] }}
          className="w-32 h-32 rounded-full bg-green-50 border-4 border-green-400 items-center justify-center mb-8"
        >
          <Text style={{ fontSize: 56 }}>{"\u2713"}</Text>
        </Animated.View>

        {/* Text content */}
        <Animated.View style={{ opacity: opacityAnim }} className="items-center">
          <Text className="text-3xl font-bold text-warmgray-800 text-center mb-4">
            You're All Set!
          </Text>
          <Text className="text-lg text-warmgray-500 text-center leading-7">
            Your companion is ready to listen.{"\n"}Start your first
            conversation.
          </Text>
        </Animated.View>
      </View>

      {/* Start Chatting Button */}
      <View className="px-6 pb-8">
        <Pressable
          onPress={handleStartChatting}
          className="w-full bg-primary-500 rounded-2xl py-4 items-center"
          style={({ pressed }) => ({ opacity: pressed ? 0.85 : 1 })}
        >
          <Text className="text-white text-base font-semibold">
            Start Chatting
          </Text>
        </Pressable>
      </View>
    </SafeAreaView>
  );
}
