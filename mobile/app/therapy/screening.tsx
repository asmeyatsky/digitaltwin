import React, { useState } from "react";
import {
  View,
  Text,
  Pressable,
  ScrollView,
  ActivityIndicator,
} from "react-native";
import { SafeAreaView } from "react-native-safe-area-context";
import { useRouter } from "expo-router";
import type { ScreeningType, ClinicalScreening } from "@/lib/api";
import { getScreeningQuestions, submitScreening } from "@/lib/api";

type ScreeningPhase = "select" | "questions" | "results";

const SCREENING_OPTIONS: {
  type: ScreeningType;
  label: string;
  description: string;
  color: string;
}[] = [
  {
    type: "PHQ9",
    label: "PHQ-9",
    description: "Patient Health Questionnaire for depression screening",
    color: "#EF4444",
  },
  {
    type: "GAD7",
    label: "GAD-7",
    description: "Generalized Anxiety Disorder assessment",
    color: "#F59E0B",
  },
  {
    type: "PSS10",
    label: "PSS-10",
    description: "Perceived Stress Scale for stress levels",
    color: "#8B5CF6",
  },
  {
    type: "WHO5",
    label: "WHO-5",
    description: "World Health Organization wellbeing index",
    color: "#10B981",
  },
];

const LIKERT_OPTIONS = [
  { value: 0, label: "Not at all" },
  { value: 1, label: "Several days" },
  { value: 2, label: "More than half the days" },
  { value: 3, label: "Nearly every day" },
];

function getSeverityColor(severity: string): string {
  const lower = severity.toLowerCase();
  if (lower.includes("minimal") || lower.includes("good") || lower.includes("low stress"))
    return "#10B981";
  if (lower.includes("mild") || lower.includes("moderate wellbeing") || lower.includes("moderate stress"))
    return "#F59E0B";
  if (lower.includes("moderate") && !lower.includes("severe"))
    return "#F97316";
  return "#EF4444";
}

function getRecommendation(severity: string): string {
  const lower = severity.toLowerCase();
  if (lower.includes("minimal") || lower.includes("good") || lower.includes("low"))
    return "Your results suggest you are doing well. Continue maintaining healthy habits and check in with yourself regularly.";
  if (lower.includes("mild"))
    return "Your results suggest mild symptoms. Consider practicing self-care techniques, and monitor how you feel over the coming weeks.";
  if (lower.includes("moderate") && !lower.includes("severe"))
    return "Your results suggest moderate symptoms. We recommend speaking with a mental health professional for guidance and support.";
  return "Your results suggest significant symptoms. We strongly recommend connecting with a therapist or mental health professional as soon as possible.";
}

export default function ScreeningScreen() {
  const router = useRouter();
  const [phase, setPhase] = useState<ScreeningPhase>("select");
  const [selectedType, setSelectedType] = useState<ScreeningType | null>(null);
  const [questions, setQuestions] = useState<string[]>([]);
  const [currentQuestion, setCurrentQuestion] = useState(0);
  const [responses, setResponses] = useState<number[]>([]);
  const [result, setResult] = useState<ClinicalScreening | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const handleSelectType = async (type: ScreeningType) => {
    setSelectedType(type);
    setLoading(true);
    setError(null);
    try {
      const res = await getScreeningQuestions(type);
      setQuestions(res.data?.questions ?? []);
      setCurrentQuestion(0);
      setResponses([]);
      setPhase("questions");
    } catch (err: any) {
      setError(err.message ?? "Failed to load questions");
    } finally {
      setLoading(false);
    }
  };

  const handleAnswer = async (value: number) => {
    const newResponses = [...responses, value];
    setResponses(newResponses);

    if (currentQuestion + 1 < questions.length) {
      setCurrentQuestion(currentQuestion + 1);
    } else {
      // Submit
      setLoading(true);
      setError(null);
      try {
        const res = await submitScreening(selectedType!, newResponses);
        setResult(res.data ?? null);
        setPhase("results");
      } catch (err: any) {
        setError(err.message ?? "Failed to submit screening");
      } finally {
        setLoading(false);
      }
    }
  };

  const handleReset = () => {
    setPhase("select");
    setSelectedType(null);
    setQuestions([]);
    setCurrentQuestion(0);
    setResponses([]);
    setResult(null);
    setError(null);
  };

  if (loading) {
    return (
      <SafeAreaView
        className="flex-1 bg-companion-bg items-center justify-center"
        edges={["bottom"]}
      >
        <ActivityIndicator size="large" color="#FF8B47" />
        <Text className="text-warmgray-500 mt-3">Loading...</Text>
      </SafeAreaView>
    );
  }

  // Phase: Select screening type
  if (phase === "select") {
    return (
      <SafeAreaView className="flex-1 bg-companion-bg" edges={["bottom"]}>
        <ScrollView contentContainerStyle={{ padding: 16, paddingBottom: 48 }}>
          <View className="mb-5">
            <Text className="text-2xl font-bold text-warmgray-800">
              Clinical Screening
            </Text>
            <Text className="text-sm text-warmgray-500 mt-1">
              Choose a validated self-assessment tool
            </Text>
          </View>

          {error && (
            <View className="bg-red-50 rounded-xl p-3 mb-4">
              <Text className="text-red-600 text-sm">{error}</Text>
            </View>
          )}

          {SCREENING_OPTIONS.map((opt) => (
            <Pressable
              key={opt.type}
              onPress={() => handleSelectType(opt.type)}
              className="bg-white rounded-2xl p-5 border border-warmgray-100 mb-3"
              style={({ pressed }) => ({ opacity: pressed ? 0.85 : 1 })}
            >
              <View className="flex-row items-center mb-1">
                <View
                  className="w-3 h-3 rounded-full mr-2"
                  style={{ backgroundColor: opt.color }}
                />
                <Text className="text-lg font-bold text-warmgray-800">
                  {opt.label}
                </Text>
              </View>
              <Text className="text-sm text-warmgray-500">
                {opt.description}
              </Text>
            </Pressable>
          ))}

          <View className="mt-4 p-4 bg-warmgray-50 rounded-xl">
            <Text className="text-xs text-warmgray-400 leading-5">
              These screenings are self-assessment tools and do not constitute a
              clinical diagnosis. For each question, indicate how often you have
              been bothered by the described experience over the last two weeks.
              Results are scored on a 0-3 scale per question.
            </Text>
          </View>
        </ScrollView>
      </SafeAreaView>
    );
  }

  // Phase: Answer questions
  if (phase === "questions") {
    const optionInfo = SCREENING_OPTIONS.find((o) => o.type === selectedType);
    const progress = ((currentQuestion + 1) / questions.length) * 100;

    return (
      <SafeAreaView className="flex-1 bg-companion-bg" edges={["bottom"]}>
        <ScrollView contentContainerStyle={{ padding: 16, paddingBottom: 48 }}>
          {/* Progress */}
          <View className="mb-2">
            <Text className="text-sm text-warmgray-500 mb-1">
              {optionInfo?.label} - Question {currentQuestion + 1} of{" "}
              {questions.length}
            </Text>
            <View className="h-2 bg-warmgray-200 rounded-full overflow-hidden">
              <View
                className="h-full rounded-full"
                style={{
                  width: `${progress}%`,
                  backgroundColor: optionInfo?.color ?? "#FF8B47",
                }}
              />
            </View>
          </View>

          {error && (
            <View className="bg-red-50 rounded-xl p-3 mb-4">
              <Text className="text-red-600 text-sm">{error}</Text>
            </View>
          )}

          {/* Question */}
          <View className="my-6">
            <Text className="text-xs text-warmgray-400 mb-2 uppercase tracking-wider">
              Over the last 2 weeks, how often have you been bothered by:
            </Text>
            <Text className="text-xl font-bold text-warmgray-800 leading-7">
              {questions[currentQuestion]}
            </Text>
          </View>

          {/* Likert Options */}
          <View className="gap-3">
            {LIKERT_OPTIONS.map((opt) => (
              <Pressable
                key={opt.value}
                onPress={() => handleAnswer(opt.value)}
                className="bg-white rounded-2xl p-4 border border-warmgray-100"
                style={({ pressed }) => ({
                  opacity: pressed ? 0.7 : 1,
                  borderColor: pressed
                    ? optionInfo?.color ?? "#FF8B47"
                    : "#F5F0EB",
                })}
              >
                <View className="flex-row items-center">
                  <View
                    className="w-8 h-8 rounded-full items-center justify-center mr-3"
                    style={{
                      backgroundColor:
                        (optionInfo?.color ?? "#FF8B47") + "20",
                    }}
                  >
                    <Text
                      className="text-sm font-bold"
                      style={{ color: optionInfo?.color ?? "#FF8B47" }}
                    >
                      {opt.value}
                    </Text>
                  </View>
                  <Text className="text-base text-warmgray-700 font-medium">
                    {opt.label}
                  </Text>
                </View>
              </Pressable>
            ))}
          </View>

          {/* Back / Cancel */}
          <View className="flex-row gap-3 mt-6">
            {currentQuestion > 0 && (
              <Pressable
                onPress={() => {
                  setCurrentQuestion(currentQuestion - 1);
                  setResponses(responses.slice(0, -1));
                }}
                className="flex-1 border border-warmgray-200 rounded-xl py-3 items-center"
                style={({ pressed }) => ({ opacity: pressed ? 0.7 : 1 })}
              >
                <Text className="text-warmgray-600 font-semibold text-sm">
                  Previous
                </Text>
              </Pressable>
            )}
            <Pressable
              onPress={handleReset}
              className="flex-1 border border-warmgray-200 rounded-xl py-3 items-center"
              style={({ pressed }) => ({ opacity: pressed ? 0.7 : 1 })}
            >
              <Text className="text-warmgray-500 font-semibold text-sm">
                Cancel
              </Text>
            </Pressable>
          </View>
        </ScrollView>
      </SafeAreaView>
    );
  }

  // Phase: Results
  if (phase === "results" && result) {
    const optionInfo = SCREENING_OPTIONS.find((o) => o.type === selectedType);
    const severityColor = getSeverityColor(result.severity);
    const recommendation = getRecommendation(result.severity);

    return (
      <SafeAreaView className="flex-1 bg-companion-bg" edges={["bottom"]}>
        <ScrollView contentContainerStyle={{ padding: 16, paddingBottom: 48 }}>
          <View className="items-center mb-6">
            <Text className="text-2xl font-bold text-warmgray-800 mb-1">
              {optionInfo?.label} Results
            </Text>
            <Text className="text-sm text-warmgray-500">
              Completed {new Date(result.completedAt).toLocaleDateString()}
            </Text>
          </View>

          {/* Score Card */}
          <View className="bg-white rounded-2xl p-6 border border-warmgray-100 items-center mb-4">
            <Text className="text-5xl font-bold" style={{ color: severityColor }}>
              {result.score}
            </Text>
            <Text className="text-sm text-warmgray-500 mt-1 mb-3">
              Total Score
            </Text>
            <View
              className="px-4 py-1.5 rounded-full"
              style={{ backgroundColor: severityColor + "20" }}
            >
              <Text
                className="text-base font-bold"
                style={{ color: severityColor }}
              >
                {result.severity}
              </Text>
            </View>
          </View>

          {/* Recommendation */}
          <View className="bg-white rounded-2xl p-5 border border-warmgray-100 mb-4">
            <Text className="text-base font-bold text-warmgray-800 mb-2">
              Recommendation
            </Text>
            <Text className="text-sm text-warmgray-600 leading-5">
              {recommendation}
            </Text>
          </View>

          {/* Disclaimer */}
          <View className="p-4 bg-warmgray-50 rounded-xl mb-6">
            <Text className="text-xs text-warmgray-400 leading-5">
              This screening is not a diagnostic tool. Results are for
              self-awareness and do not replace professional clinical evaluation.
              If you are in crisis, please contact emergency services or a crisis
              hotline immediately.
            </Text>
          </View>

          {/* Actions */}
          <View className="gap-3">
            <Pressable
              onPress={() => router.push("/therapy/therapists")}
              className="bg-primary-500 rounded-xl py-3 items-center"
              style={({ pressed }) => ({ opacity: pressed ? 0.7 : 1 })}
            >
              <Text className="text-white font-semibold text-base">
                Find a Therapist
              </Text>
            </Pressable>
            <Pressable
              onPress={handleReset}
              className="border border-warmgray-200 rounded-xl py-3 items-center"
              style={({ pressed }) => ({ opacity: pressed ? 0.7 : 1 })}
            >
              <Text className="text-warmgray-600 font-semibold text-base">
                Take Another Screening
              </Text>
            </Pressable>
          </View>
        </ScrollView>
      </SafeAreaView>
    );
  }

  return null;
}
