// Mock react-native Platform to "web" so store.ts uses memory/localStorage
// fallback instead of dynamically importing expo-secure-store.
// We use the Utilities path since jest-expo sets up react-native mocks.
jest.mock("react-native/Libraries/Utilities/Platform", () => {
  const Platform = {
    OS: "web",
    Version: 0,
    isPad: false,
    isTesting: true,
    isTV: false,
    isDisableAnimations: false,
    constants: { reactNativeVersion: { major: 0, minor: 76, patch: 6 } },
    select: (obj) => obj.web ?? obj.default ?? obj.ios,
  };
  return Platform;
});

// Mock expo-secure-store (in case it gets required)
jest.mock("expo-secure-store", () => ({
  getItemAsync: jest.fn(() => Promise.resolve(null)),
  setItemAsync: jest.fn(() => Promise.resolve()),
  deleteItemAsync: jest.fn(() => Promise.resolve()),
}));

// Mock expo-haptics
jest.mock("expo-haptics", () => ({
  impactAsync: jest.fn(),
  notificationAsync: jest.fn(),
  selectionAsync: jest.fn(),
  ImpactFeedbackStyle: { Light: "light", Medium: "medium", Heavy: "heavy" },
  NotificationFeedbackType: { Success: "success", Warning: "warning", Error: "error" },
}));

// Mock expo-av
jest.mock("expo-av", () => ({
  Audio: {
    Sound: {
      createAsync: jest.fn(() =>
        Promise.resolve({ sound: { playAsync: jest.fn(), unloadAsync: jest.fn() } })
      ),
    },
    Recording: jest.fn(),
    setAudioModeAsync: jest.fn(),
    requestPermissionsAsync: jest.fn(() =>
      Promise.resolve({ granted: true })
    ),
  },
}));

// Mock expo-camera
jest.mock("expo-camera", () => ({
  Camera: { requestCameraPermissionsAsync: jest.fn() },
  CameraView: "CameraView",
}));

// Mock expo-image-picker
jest.mock("expo-image-picker", () => ({
  launchImageLibraryAsync: jest.fn(),
  launchCameraAsync: jest.fn(),
  MediaTypeOptions: { Images: "Images" },
}));

// Mock expo-linear-gradient
jest.mock("expo-linear-gradient", () => ({
  LinearGradient: "LinearGradient",
}));

// Mock @react-three/fiber
jest.mock("@react-three/fiber", () => ({
  Canvas: "Canvas",
  useFrame: jest.fn(),
  useThree: jest.fn(() => ({ gl: {} })),
}));

// Mock @react-three/drei
jest.mock("@react-three/drei", () => ({
  OrbitControls: "OrbitControls",
  useGLTF: jest.fn(() => ({ scene: {} })),
}));

// Mock expo-file-system
jest.mock("expo-file-system", () => ({
  readAsStringAsync: jest.fn(),
  writeAsStringAsync: jest.fn(),
  cacheDirectory: "/cache/",
  EncodingType: { Base64: "base64" },
}));

// Mock global fetch
global.fetch = jest.fn();
