import React, { useCallback, useEffect, useState } from "react";
import {
  View,
  Text,
  Pressable,
  ScrollView,
  ActivityIndicator,
  TextInput,
  Alert,
} from "react-native";
import { SafeAreaView } from "react-native-safe-area-context";
import { useLocalSearchParams, useNavigation } from "expo-router";
import type {
  LearningPath,
  LearningModule,
  UserLearningProgress,
} from "@/lib/api";
import {
  getLearningPathById,
  startLearningPath,
  getCurrentLearningModule,
  completeLearningModule,
} from "@/lib/api";

const CATEGORY_COLORS: Record<string, string> = {
  EmotionalIntelligence: "#8B5CF6",
  Mindfulness: "#10B981",
  Communication: "#3B82F6",
  StressManagement: "#EF4444",
  Resilience: "#F59E0B",
  SelfCare: "#EC4899",
};

const CATEGORY_LABELS: Record<string, string> = {
  EmotionalIntelligence: "Emotional Intelligence",
  Mindfulness: "Mindfulness",
  Communication: "Communication",
  StressManagement: "Stress Management",
  Resilience: "Resilience",
  SelfCare: "Self-Care",
};

export default function PathDetailScreen() {
  const { pathId } = useLocalSearchParams<{ pathId: string }>();
  const navigation = useNavigation();

  const [path, setPath] = useState<LearningPath | null>(null);
  const [modules, setModules] = useState<LearningModule[]>([]);
  const [progress, setProgress] = useState<UserLearningProgress | null>(null);
  const [currentModule, setCurrentModule] = useState<LearningModule | null>(
    null
  );
  const [loading, setLoading] = useState(true);
  const [starting, setStarting] = useState(false);
  const [completing, setCompleting] = useState(false);
  const [reflectionNotes, setReflectionNotes] = useState("");
  const [selectedModuleIndex, setSelectedModuleIndex] = useState<number | null>(
    null
  );

  const fetchData = useCallback(async () => {
    if (!pathId) return;
    try {
      const pathRes = await getLearningPathById(pathId);
      if (pathRes.data) {
        setPath(pathRes.data.path);
        setModules(pathRes.data.modules);
        navigation.setOptions({ title: pathRes.data.path.title });
      }

      // Try to get current progress
      try {
        const currentRes = await getCurrentLearningModule(pathId);
        if (currentRes.data) {
          setProgress(currentRes.data.progress);
          setCurrentModule(currentRes.data.module);
          setSelectedModuleIndex(currentRes.data.progress.currentModuleIndex);
        }
      } catch {
        // User hasn't started this path yet
      }
    } catch {
      // ignore
    } finally {
      setLoading(false);
    }
  }, [pathId, navigation]);

  useEffect(() => {
    fetchData();
  }, [fetchData]);

  const handleStart = async () => {
    if (!pathId) return;
    setStarting(true);
    try {
      const res = await startLearningPath(pathId);
      if (res.data) {
        setProgress(res.data);
        setSelectedModuleIndex(0);
        // Fetch current module
        const currentRes = await getCurrentLearningModule(pathId);
        if (currentRes.data) {
          setCurrentModule(currentRes.data.module);
        }
      }
    } catch (err: any) {
      Alert.alert("Error", err.message || "Failed to start path");
    } finally {
      setStarting(false);
    }
  };

  const handleComplete = async () => {
    if (!pathId) return;
    setCompleting(true);
    try {
      const res = await completeLearningModule(
        pathId,
        reflectionNotes.trim() || undefined
      );
      if (res.data) {
        setProgress(res.data);
        setReflectionNotes("");

        if (res.data.completedAt) {
          Alert.alert(
            "Congratulations!",
            "You have completed this learning path!"
          );
        } else {
          // Fetch next module
          setSelectedModuleIndex(res.data.currentModuleIndex);
          const currentRes = await getCurrentLearningModule(pathId);
          if (currentRes.data) {
            setCurrentModule(currentRes.data.module);
          }
        }
      }
    } catch (err: any) {
      Alert.alert("Error", err.message || "Failed to complete module");
    } finally {
      setCompleting(false);
    }
  };

  if (loading) {
    return (
      <SafeAreaView
        className="flex-1 bg-companion-bg items-center justify-center"
        edges={["bottom"]}
      >
        <ActivityIndicator size="large" color="#FF8B47" />
      </SafeAreaView>
    );
  }

  if (!path) {
    return (
      <SafeAreaView
        className="flex-1 bg-companion-bg items-center justify-center"
        edges={["bottom"]}
      >
        <Text className="text-warmgray-500">Learning path not found</Text>
      </SafeAreaView>
    );
  }

  const color = CATEGORY_COLORS[path.category] ?? "#A89885";
  const completedModules: number[] = progress
    ? JSON.parse(progress.completedModules || "[]")
    : [];
  const isPathCompleted = progress?.completedAt != null;
  const displayModule =
    selectedModuleIndex != null ? modules[selectedModuleIndex] : currentModule;

  return (
    <SafeAreaView className="flex-1 bg-companion-bg" edges={["bottom"]}>
      <ScrollView contentContainerStyle={{ padding: 16, paddingBottom: 48 }}>
        {/* Path header */}
        <View className="bg-white rounded-2xl p-4 border border-warmgray-100 mb-4">
          <View className="flex-row items-center mb-2">
            <View
              className="px-2 py-0.5 rounded-lg mr-2"
              style={{ backgroundColor: color + "20" }}
            >
              <Text className="text-xs font-semibold" style={{ color }}>
                {CATEGORY_LABELS[path.category] ?? path.category}
              </Text>
            </View>
            <Text className="text-xs text-warmgray-400">
              ~{path.estimatedMinutes} min
            </Text>
            {isPathCompleted && (
              <View className="ml-auto px-2 py-0.5 rounded-lg bg-green-100">
                <Text className="text-xs font-semibold text-green-600">
                  Completed
                </Text>
              </View>
            )}
          </View>
          <Text className="text-xl font-bold text-warmgray-800 mb-2">
            {path.title}
          </Text>
          <Text className="text-sm text-warmgray-500">{path.description}</Text>

          {/* Progress bar */}
          {progress && (
            <View className="mt-3">
              <View className="flex-row justify-between mb-1">
                <Text className="text-xs text-warmgray-500">
                  {completedModules.length} / {path.moduleCount} modules
                  completed
                </Text>
                <Text className="text-xs font-semibold" style={{ color }}>
                  {Math.round(
                    (completedModules.length / path.moduleCount) * 100
                  )}
                  %
                </Text>
              </View>
              <View className="h-2 bg-warmgray-100 rounded-full overflow-hidden">
                <View
                  className="h-full rounded-full"
                  style={{
                    width: `${
                      (completedModules.length / path.moduleCount) * 100
                    }%`,
                    backgroundColor: isPathCompleted ? "#10B981" : color,
                  }}
                />
              </View>
            </View>
          )}

          {/* Start button */}
          {!progress && (
            <Pressable
              onPress={handleStart}
              disabled={starting}
              className="mt-4 rounded-xl py-3 items-center"
              style={({ pressed }) => ({
                opacity: pressed || starting ? 0.7 : 1,
                backgroundColor: "#3D2E22",
              })}
            >
              {starting ? (
                <ActivityIndicator size="small" color="#fff" />
              ) : (
                <Text className="text-white font-semibold">
                  Start Learning Path
                </Text>
              )}
            </Pressable>
          )}
        </View>

        {/* Module list */}
        <Text className="text-lg font-bold text-warmgray-800 mb-3">
          Modules
        </Text>
        {modules.map((mod, idx) => {
          const isCompleted = completedModules.includes(idx);
          const isCurrent = progress && idx === progress.currentModuleIndex;
          const isSelected = selectedModuleIndex === idx;

          return (
            <Pressable
              key={mod.id}
              onPress={() => setSelectedModuleIndex(idx)}
              className="bg-white rounded-xl p-3 border mb-2"
              style={({ pressed }) => ({
                opacity: pressed ? 0.85 : 1,
                borderColor: isSelected
                  ? color
                  : isCurrent
                  ? color + "60"
                  : "#F5F0EB",
                borderWidth: isSelected ? 2 : 1,
              })}
            >
              <View className="flex-row items-center">
                {/* Completion indicator */}
                <View
                  className="w-6 h-6 rounded-full mr-3 items-center justify-center"
                  style={{
                    backgroundColor: isCompleted
                      ? "#10B981"
                      : isCurrent
                      ? color + "30"
                      : "#F5F0EB",
                  }}
                >
                  {isCompleted ? (
                    <Text className="text-white text-xs font-bold">
                      {"\u2713"}
                    </Text>
                  ) : (
                    <Text
                      className="text-xs font-semibold"
                      style={{
                        color: isCurrent ? color : "#A89885",
                      }}
                    >
                      {idx + 1}
                    </Text>
                  )}
                </View>
                <View className="flex-1">
                  <Text
                    className="text-sm font-semibold"
                    style={{
                      color: isCompleted
                        ? "#10B981"
                        : isCurrent
                        ? color
                        : "#3D2E22",
                    }}
                  >
                    {mod.title}
                  </Text>
                </View>
                {isCurrent && !isCompleted && (
                  <View
                    className="px-2 py-0.5 rounded-lg"
                    style={{ backgroundColor: color + "20" }}
                  >
                    <Text
                      className="text-xs font-semibold"
                      style={{ color }}
                    >
                      Current
                    </Text>
                  </View>
                )}
              </View>
            </Pressable>
          );
        })}

        {/* Current module content */}
        {displayModule && progress && (
          <View className="mt-4">
            <View className="bg-white rounded-2xl p-4 border border-warmgray-100 mb-4">
              <Text className="text-lg font-bold text-warmgray-800 mb-3">
                {displayModule.title}
              </Text>
              <Text className="text-sm text-warmgray-600 leading-5">
                {displayModule.content}
              </Text>
            </View>

            {/* Exercise prompt */}
            <View
              className="rounded-2xl p-4 mb-4"
              style={{ backgroundColor: color + "10" }}
            >
              <Text
                className="text-sm font-bold mb-2"
                style={{ color }}
              >
                Reflection Exercise
              </Text>
              <Text className="text-sm text-warmgray-700 leading-5">
                {displayModule.exercisePrompt}
              </Text>
            </View>

            {/* Reflection notes input + complete button */}
            {selectedModuleIndex === progress.currentModuleIndex &&
              !isPathCompleted &&
              !completedModules.includes(selectedModuleIndex!) && (
                <View>
                  <Text className="text-sm font-semibold text-warmgray-700 mb-2">
                    Your Reflection (optional)
                  </Text>
                  <TextInput
                    className="bg-white border border-warmgray-200 rounded-xl px-4 py-3 text-sm text-warmgray-800 mb-3"
                    placeholder="Write your thoughts here..."
                    placeholderTextColor="#A89885"
                    multiline
                    numberOfLines={4}
                    textAlignVertical="top"
                    style={{ minHeight: 100 }}
                    value={reflectionNotes}
                    onChangeText={setReflectionNotes}
                  />
                  <Pressable
                    onPress={handleComplete}
                    disabled={completing}
                    className="rounded-xl py-3 items-center"
                    style={({ pressed }) => ({
                      opacity: pressed || completing ? 0.7 : 1,
                      backgroundColor: color,
                    })}
                  >
                    {completing ? (
                      <ActivityIndicator size="small" color="#fff" />
                    ) : (
                      <Text className="text-white font-semibold">
                        Complete & Continue
                      </Text>
                    )}
                  </Pressable>
                </View>
              )}
          </View>
        )}
      </ScrollView>
    </SafeAreaView>
  );
}
