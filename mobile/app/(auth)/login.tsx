import React, { useState } from "react";
import {
  View,
  Text,
  TextInput,
  Pressable,
  KeyboardAvoidingView,
  Platform,
  ScrollView,
  ActivityIndicator,
} from "react-native";
import { Link } from "expo-router";
import { SafeAreaView } from "react-native-safe-area-context";
import { useAuth } from "@/lib/hooks";

export default function LoginScreen() {
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const { login, isLoggingIn, loginError } = useAuth();

  const handleLogin = async () => {
    if (!email.trim() || !password.trim()) return;
    try {
      await login({ email: email.trim(), password });
    } catch {
      // Error is captured in loginError
    }
  };

  const isDisabled = isLoggingIn || !email.trim() || !password.trim();

  return (
    <SafeAreaView className="flex-1 bg-companion-bg">
      <KeyboardAvoidingView
        behavior={Platform.OS === "ios" ? "padding" : "height"}
        className="flex-1"
      >
        <ScrollView
          contentContainerStyle={{
            flexGrow: 1,
            justifyContent: "center",
            padding: 24,
          }}
          keyboardShouldPersistTaps="handled"
        >
          {/* Logo / Branding */}
          <View className="items-center mb-10">
            <View className="w-20 h-20 rounded-full bg-primary-500 items-center justify-center mb-4">
              <Text className="text-white text-3xl font-bold">DT</Text>
            </View>
            <Text className="text-3xl font-bold text-warmgray-800">
              Welcome Back
            </Text>
            <Text className="text-base text-warmgray-500 mt-2 text-center">
              Your emotional companion is here for you
            </Text>
          </View>

          {/* Form */}
          <View className="gap-4">
            <View>
              <Text className="text-sm font-medium text-warmgray-600 mb-1.5 ml-1">
                Email
              </Text>
              <TextInput
                className="bg-white border border-warmgray-200 rounded-2xl px-4 py-3.5 text-base text-warmgray-800"
                placeholder="your@email.com"
                placeholderTextColor="#A89885"
                keyboardType="email-address"
                autoCapitalize="none"
                autoCorrect={false}
                autoComplete="email"
                value={email}
                onChangeText={setEmail}
                editable={!isLoggingIn}
              />
            </View>

            <View>
              <Text className="text-sm font-medium text-warmgray-600 mb-1.5 ml-1">
                Password
              </Text>
              <TextInput
                className="bg-white border border-warmgray-200 rounded-2xl px-4 py-3.5 text-base text-warmgray-800"
                placeholder="Enter your password"
                placeholderTextColor="#A89885"
                secureTextEntry
                autoComplete="password"
                value={password}
                onChangeText={setPassword}
                editable={!isLoggingIn}
                onSubmitEditing={handleLogin}
              />
            </View>

            {loginError && (
              <View className="bg-red-50 border border-red-200 rounded-2xl px-4 py-3">
                <Text className="text-red-600 text-sm text-center">
                  {loginError.message}
                </Text>
              </View>
            )}

            <Pressable
              onPress={handleLogin}
              disabled={isDisabled}
              className={`rounded-2xl py-4 items-center mt-2 ${
                isDisabled ? "bg-primary-300" : "bg-primary-500"
              }`}
              style={({ pressed }) => ({
                opacity: pressed && !isDisabled ? 0.85 : 1,
              })}
            >
              {isLoggingIn ? (
                <ActivityIndicator color="#fff" />
              ) : (
                <Text className="text-white text-base font-semibold">
                  Sign In
                </Text>
              )}
            </Pressable>
          </View>

          {/* Footer */}
          <View className="flex-row justify-center mt-8">
            <Text className="text-warmgray-500 text-sm">
              Don't have an account?{" "}
            </Text>
            <Link href="/(auth)/register" asChild>
              <Pressable>
                <Text className="text-primary-500 text-sm font-semibold">
                  Sign Up
                </Text>
              </Pressable>
            </Link>
          </View>
        </ScrollView>
      </KeyboardAvoidingView>
    </SafeAreaView>
  );
}
