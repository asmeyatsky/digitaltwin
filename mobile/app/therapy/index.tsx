import React, { useCallback, useEffect, useState } from "react";
import {
  View,
  Text,
  Pressable,
  ScrollView,
  ActivityIndicator,
  RefreshControl,
} from "react-native";
import { SafeAreaView } from "react-native-safe-area-context";
import { useRouter } from "expo-router";
import {
  getTherapySessions,
  getScreeningHistory,
  getTherapistReferrals,
} from "@/lib/api";
import type {
  TherapySession,
  ClinicalScreening,
  TherapistReferral,
} from "@/lib/api";

const URGENCY_COLORS: Record<string, string> = {
  Low: "#10B981",
  Medium: "#F59E0B",
  High: "#EF4444",
  Critical: "#991B1B",
};

function HubCard({
  title,
  subtitle,
  badge,
  color,
  onPress,
}: {
  title: string;
  subtitle: string;
  badge?: string;
  color: string;
  onPress: () => void;
}) {
  return (
    <Pressable
      onPress={onPress}
      className="bg-white rounded-2xl p-5 border border-warmgray-100 mb-3"
      style={({ pressed }) => ({ opacity: pressed ? 0.85 : 1 })}
    >
      <View className="flex-row items-center justify-between mb-1">
        <Text className="text-lg font-bold text-warmgray-800">{title}</Text>
        {badge ? (
          <View
            className="px-2 py-0.5 rounded-full"
            style={{ backgroundColor: color + "20" }}
          >
            <Text className="text-xs font-semibold" style={{ color }}>
              {badge}
            </Text>
          </View>
        ) : null}
      </View>
      <Text className="text-sm text-warmgray-500">{subtitle}</Text>
    </Pressable>
  );
}

export default function TherapyHubScreen() {
  const router = useRouter();
  const [sessions, setSessions] = useState<TherapySession[]>([]);
  const [screenings, setScreenings] = useState<ClinicalScreening[]>([]);
  const [referrals, setReferrals] = useState<TherapistReferral[]>([]);
  const [loading, setLoading] = useState(true);
  const [refreshing, setRefreshing] = useState(false);

  const fetchData = useCallback(async () => {
    try {
      const [sessionsRes, screeningsRes, referralsRes] = await Promise.all([
        getTherapySessions(1, 5),
        getScreeningHistory(),
        getTherapistReferrals(),
      ]);
      setSessions(sessionsRes.data?.sessions ?? []);
      setScreenings(screeningsRes.data ?? []);
      setReferrals(referralsRes.data ?? []);
    } catch {
      // ignore
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    fetchData();
  }, [fetchData]);

  const onRefresh = async () => {
    setRefreshing(true);
    await fetchData();
    setRefreshing(false);
  };

  const upcomingSessions = sessions.filter((s) => s.status === "Scheduled");
  const unacknowledgedReferrals = referrals.filter((r) => !r.isAcknowledged);

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

  return (
    <SafeAreaView className="flex-1 bg-companion-bg" edges={["bottom"]}>
      <ScrollView
        contentContainerStyle={{ padding: 16, paddingBottom: 48 }}
        refreshControl={
          <RefreshControl refreshing={refreshing} onRefresh={onRefresh} />
        }
      >
        {/* Header */}
        <View className="mb-5">
          <Text className="text-2xl font-bold text-warmgray-800">
            Therapy & Wellness
          </Text>
          <Text className="text-sm text-warmgray-500 mt-1">
            Professional support and clinical self-assessment tools
          </Text>
        </View>

        {/* Hub Cards */}
        <HubCard
          title="Find a Therapist"
          subtitle="Browse verified therapists and book a session"
          color="#3B82F6"
          onPress={() => router.push("/therapy/therapists")}
        />

        <HubCard
          title="Take a Screening"
          subtitle="PHQ-9, GAD-7, PSS-10, WHO-5 self-assessments"
          badge={screenings.length > 0 ? `${screenings.length} completed` : undefined}
          color="#8B5CF6"
          onPress={() => router.push("/therapy/screening")}
        />

        <HubCard
          title="My Sessions"
          subtitle="View and manage your therapy appointments"
          badge={
            upcomingSessions.length > 0
              ? `${upcomingSessions.length} upcoming`
              : undefined
          }
          color="#10B981"
          onPress={() => router.push("/therapy/therapists")}
        />

        <HubCard
          title="Referrals"
          subtitle="Therapist referrals and recommendations"
          badge={
            unacknowledgedReferrals.length > 0
              ? `${unacknowledgedReferrals.length} new`
              : undefined
          }
          color="#F59E0B"
          onPress={() => router.push("/therapy/therapists")}
        />

        {/* Recent Screenings */}
        {screenings.length > 0 && (
          <View className="mt-4">
            <Text className="text-lg font-bold text-warmgray-800 mb-3">
              Recent Screenings
            </Text>
            {screenings.slice(0, 3).map((s) => (
              <View
                key={s.id}
                className="bg-white rounded-2xl p-4 border border-warmgray-100 mb-2"
              >
                <View className="flex-row items-center justify-between mb-1">
                  <Text className="text-base font-semibold text-warmgray-800">
                    {s.type}
                  </Text>
                  <Text className="text-sm text-warmgray-400">
                    Score: {s.score}
                  </Text>
                </View>
                <Text className="text-sm text-warmgray-500">
                  Severity: {s.severity}
                </Text>
                <Text className="text-xs text-warmgray-400 mt-1">
                  {new Date(s.completedAt).toLocaleDateString()}
                </Text>
              </View>
            ))}
          </View>
        )}

        {/* Recent Referrals */}
        {referrals.length > 0 && (
          <View className="mt-4">
            <Text className="text-lg font-bold text-warmgray-800 mb-3">
              Referrals
            </Text>
            {referrals.slice(0, 3).map((r) => (
              <View
                key={r.id}
                className="bg-white rounded-2xl p-4 border border-warmgray-100 mb-2"
              >
                <View className="flex-row items-center justify-between mb-1">
                  <Text className="text-base font-semibold text-warmgray-800">
                    {r.reason}
                  </Text>
                  <View
                    className="px-2 py-0.5 rounded-full"
                    style={{
                      backgroundColor:
                        (URGENCY_COLORS[r.urgency] ?? "#A89885") + "20",
                    }}
                  >
                    <Text
                      className="text-xs font-semibold"
                      style={{ color: URGENCY_COLORS[r.urgency] ?? "#A89885" }}
                    >
                      {r.urgency}
                    </Text>
                  </View>
                </View>
                <Text className="text-xs text-warmgray-400">
                  {new Date(r.createdAt).toLocaleDateString()}
                </Text>
              </View>
            ))}
          </View>
        )}
      </ScrollView>
    </SafeAreaView>
  );
}
