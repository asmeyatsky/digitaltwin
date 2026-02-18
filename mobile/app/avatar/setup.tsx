import React, { useState } from "react";
import { View, Text, Pressable, Image, ActivityIndicator, Alert, Platform } from "react-native";
import { SafeAreaView } from "react-native-safe-area-context";
import { useRouter } from "expo-router";
import * as ImagePicker from "expo-image-picker";
import { useAvatar } from "@/lib/hooks";
import AvatarView from "@/components/AvatarView";

type Step = "intro" | "capture" | "generating" | "complete";

export default function AvatarSetupScreen() {
  const router = useRouter();
  const { avatarUrl, isGenerating, avatarStatus, generateAvatar, generateError } = useAvatar();
  const [step, setStep] = useState<Step>("intro");
  const [imageUri, setImageUri] = useState<string | null>(null);

  const handleCapture = async () => {
    const { status } = await ImagePicker.requestCameraPermissionsAsync();
    if (status !== "granted") {
      const msg = "Camera permission is needed to take a selfie for your avatar.";
      if (Platform.OS === "web") window.alert(msg);
      else Alert.alert("Permission Required", msg);
      return;
    }

    const result = await ImagePicker.launchCameraAsync({
      allowsEditing: true,
      aspect: [1, 1],
      quality: 0.8,
    });

    if (!result.canceled && result.assets[0]) {
      setImageUri(result.assets[0].uri);
      setStep("capture");
    }
  };

  const handlePickFromLibrary = async () => {
    const { status } = await ImagePicker.requestMediaLibraryPermissionsAsync();
    if (status !== "granted") {
      const msg = "Photo library access is needed to select a photo for your avatar.";
      if (Platform.OS === "web") window.alert(msg);
      else Alert.alert("Permission Required", msg);
      return;
    }

    const result = await ImagePicker.launchImageLibraryAsync({
      allowsEditing: true,
      aspect: [1, 1],
      quality: 0.8,
    });

    if (!result.canceled && result.assets[0]) {
      setImageUri(result.assets[0].uri);
      setStep("capture");
    }
  };

  const handleGenerate = async () => {
    if (!imageUri) return;

    setStep("generating");

    try {
      const formData = new FormData();

      if (Platform.OS === "web") {
        const response = await fetch(imageUri);
        const blob = await response.blob();
        formData.append("image", blob, "selfie.jpg");
      } else {
        formData.append("image", {
          uri: imageUri,
          type: "image/jpeg",
          name: "selfie.jpg",
        } as any);
      }

      formData.append("style", "realistic");

      await generateAvatar(formData);
    } catch {
      setStep("capture");
      const msg = generateError?.message ?? "Failed to start avatar generation. Please try again.";
      if (Platform.OS === "web") window.alert(msg);
      else Alert.alert("Error", msg);
    }
  };

  // Poll status and update step
  React.useEffect(() => {
    if (avatarStatus?.status === "completed") {
      setStep("complete");
    } else if (avatarStatus?.status === "failed") {
      setStep("capture");
      const msg = avatarStatus.error ?? "Avatar generation failed. Please try again.";
      if (Platform.OS === "web") window.alert(msg);
      else Alert.alert("Generation Failed", msg);
    }
  }, [avatarStatus?.status]);

  return (
    <SafeAreaView className="flex-1 bg-companion-bg" edges={["bottom"]}>
      <View className="flex-1 px-6 justify-center items-center">
        {step === "intro" && (
          <>
            <AvatarView size={120} />
            <Text className="text-2xl font-bold text-warmgray-800 mt-6 text-center">
              Create Your 3D Avatar
            </Text>
            <Text className="text-base text-warmgray-500 mt-3 text-center leading-6 max-w-xs">
              Take a selfie or choose a photo and we'll generate a personalized 3D
              avatar for your companion.
            </Text>

            <View className="w-full gap-3 mt-8">
              <Pressable
                onPress={handleCapture}
                className="bg-primary-500 rounded-2xl py-4 items-center"
                style={({ pressed }) => ({ opacity: pressed ? 0.8 : 1 })}
              >
                <Text className="text-white font-bold text-base">Take a Selfie</Text>
              </Pressable>

              <Pressable
                onPress={handlePickFromLibrary}
                className="bg-white border border-warmgray-200 rounded-2xl py-4 items-center"
                style={({ pressed }) => ({ opacity: pressed ? 0.8 : 1 })}
              >
                <Text className="text-warmgray-700 font-semibold text-base">
                  Choose from Library
                </Text>
              </Pressable>
            </View>
          </>
        )}

        {step === "capture" && imageUri && (
          <>
            <Image
              source={{ uri: imageUri }}
              className="w-40 h-40 rounded-3xl"
              style={{ width: 160, height: 160, borderRadius: 24 }}
            />
            <Text className="text-xl font-bold text-warmgray-800 mt-6 text-center">
              Looking great!
            </Text>
            <Text className="text-base text-warmgray-500 mt-2 text-center">
              Ready to generate your 3D avatar?
            </Text>

            <View className="w-full gap-3 mt-8">
              <Pressable
                onPress={handleGenerate}
                className="bg-primary-500 rounded-2xl py-4 items-center"
                style={({ pressed }) => ({ opacity: pressed ? 0.8 : 1 })}
              >
                <Text className="text-white font-bold text-base">Generate Avatar</Text>
              </Pressable>

              <Pressable
                onPress={() => {
                  setImageUri(null);
                  setStep("intro");
                }}
                className="bg-white border border-warmgray-200 rounded-2xl py-4 items-center"
                style={({ pressed }) => ({ opacity: pressed ? 0.8 : 1 })}
              >
                <Text className="text-warmgray-700 font-semibold text-base">
                  Retake Photo
                </Text>
              </Pressable>
            </View>
          </>
        )}

        {step === "generating" && (
          <>
            <ActivityIndicator size="large" color="#FF8B47" />
            <Text className="text-xl font-bold text-warmgray-800 mt-6 text-center">
              Generating Your Avatar
            </Text>
            <Text className="text-base text-warmgray-500 mt-2 text-center">
              This may take a minute...
            </Text>
            {avatarStatus && (
              <Text className="text-sm text-warmgray-400 mt-4 capitalize">
                Status: {avatarStatus.status}
              </Text>
            )}
          </>
        )}

        {step === "complete" && (
          <>
            <View className="bg-green-50 rounded-full w-20 h-20 items-center justify-center mb-4">
              <Text className="text-4xl">&#10003;</Text>
            </View>
            <Text className="text-2xl font-bold text-warmgray-800 text-center">
              Avatar Created!
            </Text>
            <Text className="text-base text-warmgray-500 mt-2 text-center">
              Your 3D avatar is ready. It will appear in your chat.
            </Text>

            <Pressable
              onPress={() => router.back()}
              className="bg-primary-500 rounded-2xl py-4 items-center w-full mt-8"
              style={({ pressed }) => ({ opacity: pressed ? 0.8 : 1 })}
            >
              <Text className="text-white font-bold text-base">Done</Text>
            </Pressable>
          </>
        )}
      </View>
    </SafeAreaView>
  );
}
