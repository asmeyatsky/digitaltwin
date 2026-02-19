import React, { useCallback, useEffect, useState } from "react";
import {
  View,
  Text,
  Pressable,
  ScrollView,
  ActivityIndicator,
  TextInput,
  RefreshControl,
  Alert,
} from "react-native";
import { SafeAreaView } from "react-native-safe-area-context";
import type { TherapistProfile } from "@/lib/api";
import { getTherapists, bookTherapySession } from "@/lib/api";

const SPECIALIZATIONS = [
  "Anxiety",
  "Depression",
  "Trauma",
  "Relationships",
  "Grief",
  "Stress",
  "Self-Esteem",
  "Addiction",
];

function TherapistCard({
  therapist,
  onBook,
}: {
  therapist: TherapistProfile;
  onBook: () => void;
}) {
  let specializations: string[] = [];
  try {
    specializations = JSON.parse(therapist.specializations);
  } catch {
    // ignore
  }

  return (
    <View className="bg-white rounded-2xl p-4 border border-warmgray-100 mb-3">
      <View className="flex-row items-center justify-between mb-2">
        <View className="flex-1">
          <View className="flex-row items-center gap-2">
            <Text className="text-base font-bold text-warmgray-800">
              {therapist.name}
            </Text>
            {therapist.isVerified && (
              <View className="bg-blue-100 px-2 py-0.5 rounded-full">
                <Text className="text-xs font-semibold text-blue-600">
                  Verified
                </Text>
              </View>
            )}
          </View>
          <Text className="text-sm text-warmgray-500 mt-0.5">
            {therapist.credentials}
          </Text>
        </View>
        <View className="items-end">
          <Text className="text-base font-bold text-warmgray-800">
            ${therapist.ratePerSession}
          </Text>
          <Text className="text-xs text-warmgray-400">per session</Text>
        </View>
      </View>

      <Text className="text-sm text-warmgray-600 mb-2" numberOfLines={3}>
        {therapist.bio}
      </Text>

      {specializations.length > 0 && (
        <View className="flex-row flex-wrap gap-1 mb-3">
          {specializations.map((spec: string) => (
            <View
              key={spec}
              className="px-2 py-0.5 rounded-lg bg-warmgray-100"
            >
              <Text className="text-xs text-warmgray-600">{spec}</Text>
            </View>
          ))}
        </View>
      )}

      <Pressable
        onPress={onBook}
        className="bg-primary-500 rounded-xl py-2.5 items-center"
        style={({ pressed }) => ({ opacity: pressed ? 0.7 : 1 })}
      >
        <Text className="text-white font-semibold text-sm">Book Session</Text>
      </Pressable>
    </View>
  );
}

export default function TherapistsScreen() {
  const [therapists, setTherapists] = useState<TherapistProfile[]>([]);
  const [loading, setLoading] = useState(true);
  const [refreshing, setRefreshing] = useState(false);
  const [search, setSearch] = useState("");
  const [selectedSpec, setSelectedSpec] = useState<string | null>(null);
  const [searching, setSearching] = useState(false);

  const fetchTherapists = useCallback(
    async (spec?: string) => {
      try {
        const res = await getTherapists(spec ?? undefined, 1, 50);
        setTherapists(res.data?.therapists ?? []);
      } catch {
        // ignore
      } finally {
        setLoading(false);
        setSearching(false);
      }
    },
    []
  );

  useEffect(() => {
    fetchTherapists(selectedSpec ?? undefined);
  }, [fetchTherapists, selectedSpec]);

  useEffect(() => {
    if (!search.trim()) return;
    setSearching(true);
    const timeout = setTimeout(() => {
      fetchTherapists(search.trim());
    }, 400);
    return () => clearTimeout(timeout);
  }, [search, fetchTherapists]);

  const onRefresh = async () => {
    setRefreshing(true);
    await fetchTherapists(selectedSpec ?? undefined);
    setRefreshing(false);
  };

  const handleBook = (therapist: TherapistProfile) => {
    // Schedule for tomorrow at 10:00 AM as a default
    const tomorrow = new Date();
    tomorrow.setDate(tomorrow.getDate() + 1);
    tomorrow.setHours(10, 0, 0, 0);

    Alert.alert(
      "Book Session",
      `Book a session with ${therapist.name} for ${tomorrow.toLocaleDateString()} at 10:00 AM?`,
      [
        { text: "Cancel", style: "cancel" },
        {
          text: "Confirm",
          onPress: async () => {
            try {
              await bookTherapySession(therapist.id, tomorrow.toISOString());
              Alert.alert("Success", "Session booked successfully!");
            } catch (err: any) {
              Alert.alert("Error", err.message ?? "Failed to book session");
            }
          },
        },
      ]
    );
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

  return (
    <SafeAreaView className="flex-1 bg-companion-bg" edges={["bottom"]}>
      <ScrollView
        contentContainerStyle={{ padding: 16, paddingBottom: 48 }}
        refreshControl={
          <RefreshControl refreshing={refreshing} onRefresh={onRefresh} />
        }
      >
        {/* Header */}
        <View className="mb-4">
          <Text className="text-2xl font-bold text-warmgray-800">
            Find a Therapist
          </Text>
          <Text className="text-sm text-warmgray-500 mt-1">
            Browse verified professionals and book a session
          </Text>
        </View>

        {/* Search */}
        <TextInput
          className="border border-warmgray-200 bg-white rounded-xl px-4 py-3 text-base text-warmgray-800 mb-3"
          placeholder="Search by name or specialization..."
          placeholderTextColor="#A89885"
          value={search}
          onChangeText={setSearch}
        />

        {/* Specialization Filter */}
        <ScrollView
          horizontal
          showsHorizontalScrollIndicator={false}
          className="mb-4"
          contentContainerStyle={{ gap: 8 }}
        >
          <Pressable
            onPress={() => {
              setSelectedSpec(null);
              setSearch("");
            }}
            className="px-4 py-1.5 rounded-full"
            style={{
              backgroundColor: !selectedSpec ? "#3D2E22" : "#F5F0EB",
            }}
          >
            <Text
              className="text-sm font-semibold"
              style={{ color: !selectedSpec ? "#fff" : "#A89885" }}
            >
              All
            </Text>
          </Pressable>
          {SPECIALIZATIONS.map((spec) => (
            <Pressable
              key={spec}
              onPress={() =>
                setSelectedSpec(selectedSpec === spec ? null : spec)
              }
              className="px-4 py-1.5 rounded-full"
              style={{
                backgroundColor:
                  selectedSpec === spec ? "#3B82F6" : "#3B82F620",
              }}
            >
              <Text
                className="text-sm font-semibold"
                style={{
                  color: selectedSpec === spec ? "#fff" : "#3B82F6",
                }}
              >
                {spec}
              </Text>
            </Pressable>
          ))}
        </ScrollView>

        {/* Results */}
        {searching ? (
          <ActivityIndicator size="small" color="#FF8B47" />
        ) : therapists.length > 0 ? (
          therapists.map((t) => (
            <TherapistCard
              key={t.id}
              therapist={t}
              onBook={() => handleBook(t)}
            />
          ))
        ) : (
          <View className="items-center py-12">
            <Text className="text-warmgray-400 text-sm">
              No therapists found
            </Text>
          </View>
        )}
      </ScrollView>
    </SafeAreaView>
  );
}
