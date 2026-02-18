import { ExpoConfig, ConfigContext } from "expo/config";

export default ({ config }: ConfigContext): ExpoConfig => ({
  ...config,
  name: "Digital Twin Companion",
  slug: "digitaltwin-companion",
  version: "1.0.0",
  orientation: "portrait",
  icon: "./assets/icon.png",
  scheme: "digitaltwin",
  userInterfaceStyle: "automatic",
  newArchEnabled: true,
  splash: {
    image: "./assets/splash.png",
    resizeMode: "contain",
    backgroundColor: "#F5E6D3",
  },
  ios: {
    supportsTablet: true,
    bundleIdentifier: "com.digitaltwin.companion",
    infoPlist: {
      NSMicrophoneUsageDescription:
        "We need access to your microphone for voice conversations with your companion.",
      NSCameraUsageDescription:
        "We need access to your camera for avatar customization and emotion detection.",
      NSPhotoLibraryUsageDescription:
        "We need access to your photos for avatar generation.",
    },
  },
  android: {
    adaptiveIcon: {
      foregroundImage: "./assets/adaptive-icon.png",
      backgroundColor: "#F5E6D3",
    },
    package: "com.digitaltwin.companion",
    permissions: ["RECORD_AUDIO", "CAMERA"],
  },
  web: {
    bundler: "metro",
    output: "single",
    favicon: "./assets/favicon.png",
  },
  plugins: [
    "expo-router",
    "expo-secure-store",
    [
      "expo-av",
      {
        microphonePermission:
          "Allow Digital Twin Companion to access your microphone for voice conversations.",
      },
    ],
    [
      "expo-camera",
      {
        cameraPermission:
          "Allow Digital Twin Companion to access your camera for avatar customization and emotion detection.",
      },
    ],
    [
      "expo-image-picker",
      {
        photosPermission:
          "Allow Digital Twin Companion to access your photos for avatar generation.",
        cameraPermission:
          "Allow Digital Twin Companion to use your camera for avatar selfies.",
      },
    ],
    [
      "@stripe/stripe-react-native",
      {
        merchantIdentifier: "merchant.com.digitaltwin.companion",
        enableGooglePay: true,
      },
    ],
    "expo-haptics",
    [
      "react-native-reanimated/plugin",
    ],
  ],
  experiments: {
    typedRoutes: true,
  },
});
