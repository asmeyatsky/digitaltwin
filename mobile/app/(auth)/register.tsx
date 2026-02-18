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

export default function RegisterScreen() {
  const [displayName, setDisplayName] = useState("");
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [confirmPassword, setConfirmPassword] = useState("");
  const { register, isRegistering, registerError } = useAuth();

  const passwordMismatch =
    confirmPassword.length > 0 && password !== confirmPassword;

  const isDisabled =
    isRegistering ||
    !displayName.trim() ||
    !email.trim() ||
    !password.trim() ||
    password !== confirmPassword;

  const handleRegister = async () => {
    if (isDisabled) return;
    try {
      await register({
        email: email.trim(),
        password,
        displayName: displayName.trim(),
      });
    } catch {
      // Error is captured in registerError
    }
  };

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
          {/* Header */}
          <View className="items-center mb-10">
            <View className="w-20 h-20 rounded-full bg-primary-500 items-center justify-center mb-4">
              <Text className="text-white text-3xl font-bold">DT</Text>
            </View>
            <Text className="text-3xl font-bold text-warmgray-800">
              Create Account
            </Text>
            <Text className="text-base text-warmgray-500 mt-2 text-center">
              Start your journey with your digital companion
            </Text>
          </View>

          {/* Form */}
          <View className="gap-4">
            <View>
              <Text className="text-sm font-medium text-warmgray-600 mb-1.5 ml-1">
                Display Name
              </Text>
              <TextInput
                className="bg-white border border-warmgray-200 rounded-2xl px-4 py-3.5 text-base text-warmgray-800"
                placeholder="How should we call you?"
                placeholderTextColor="#A89885"
                autoCapitalize="words"
                autoCorrect={false}
                value={displayName}
                onChangeText={setDisplayName}
                editable={!isRegistering}
              />
            </View>

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
                editable={!isRegistering}
              />
            </View>

            <View>
              <Text className="text-sm font-medium text-warmgray-600 mb-1.5 ml-1">
                Password
              </Text>
              <TextInput
                className="bg-white border border-warmgray-200 rounded-2xl px-4 py-3.5 text-base text-warmgray-800"
                placeholder="At least 8 characters"
                placeholderTextColor="#A89885"
                secureTextEntry
                autoComplete="new-password"
                value={password}
                onChangeText={setPassword}
                editable={!isRegistering}
              />
            </View>

            <View>
              <Text className="text-sm font-medium text-warmgray-600 mb-1.5 ml-1">
                Confirm Password
              </Text>
              <TextInput
                className={`bg-white border rounded-2xl px-4 py-3.5 text-base text-warmgray-800 ${
                  passwordMismatch
                    ? "border-red-400"
                    : "border-warmgray-200"
                }`}
                placeholder="Re-enter your password"
                placeholderTextColor="#A89885"
                secureTextEntry
                autoComplete="new-password"
                value={confirmPassword}
                onChangeText={setConfirmPassword}
                editable={!isRegistering}
                onSubmitEditing={handleRegister}
              />
              {passwordMismatch && (
                <Text className="text-red-500 text-xs mt-1 ml-1">
                  Passwords do not match
                </Text>
              )}
            </View>

            {registerError && (
              <View className="bg-red-50 border border-red-200 rounded-2xl px-4 py-3">
                <Text className="text-red-600 text-sm text-center">
                  {registerError.message}
                </Text>
              </View>
            )}

            <Pressable
              onPress={handleRegister}
              disabled={isDisabled}
              className={`rounded-2xl py-4 items-center mt-2 ${
                isDisabled ? "bg-primary-300" : "bg-primary-500"
              }`}
              style={({ pressed }) => ({
                opacity: pressed && !isDisabled ? 0.85 : 1,
              })}
            >
              {isRegistering ? (
                <ActivityIndicator color="#fff" />
              ) : (
                <Text className="text-white text-base font-semibold">
                  Create Account
                </Text>
              )}
            </Pressable>
          </View>

          {/* Footer */}
          <View className="flex-row justify-center mt-8">
            <Text className="text-warmgray-500 text-sm">
              Already have an account?{" "}
            </Text>
            <Link href="/(auth)/login" asChild>
              <Pressable>
                <Text className="text-primary-500 text-sm font-semibold">
                  Sign In
                </Text>
              </Pressable>
            </Link>
          </View>
        </ScrollView>
      </KeyboardAvoidingView>
    </SafeAreaView>
  );
}
