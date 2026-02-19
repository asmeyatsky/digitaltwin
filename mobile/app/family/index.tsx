import React, { useCallback, useEffect, useState } from "react";
import {
  View,
  Text,
  Pressable,
  ScrollView,
  ActivityIndicator,
  Alert,
  TextInput,
  RefreshControl,
} from "react-native";
import { SafeAreaView } from "react-native-safe-area-context";
import { useRouter } from "expo-router";
import type {
  Family,
  FamilyMember,
  FamilyRole,
  FamilyWithMembers,
} from "@/lib/api";
import {
  createFamily,
  getFamily,
  removeFamilyMember,
  joinFamily,
} from "@/lib/api";

const ROLE_COLORS: Record<string, string> = {
  Owner: "#8B5CF6",
  Adult: "#FF8B47",
  Child: "#38BDF8",
};

function MemberCard({
  member,
  isOwner,
  onRemove,
}: {
  member: FamilyMember;
  isOwner: boolean;
  onRemove?: () => void;
}) {
  return (
    <View className="flex-row items-center bg-white rounded-2xl p-4 border border-warmgray-100">
      <View
        className="w-10 h-10 rounded-full items-center justify-center mr-3"
        style={{ backgroundColor: (ROLE_COLORS[member.role] ?? "#A89885") + "20" }}
      >
        <Text
          className="text-base font-bold"
          style={{ color: ROLE_COLORS[member.role] ?? "#A89885" }}
        >
          {member.role.charAt(0)}
        </Text>
      </View>
      <View className="flex-1">
        <Text className="text-sm font-semibold text-warmgray-800">
          {member.userId.substring(0, 8)}...
        </Text>
        <Text className="text-xs text-warmgray-500 capitalize">{member.role}</Text>
      </View>
      {isOwner && member.role !== "Owner" && onRemove && (
        <Pressable
          onPress={onRemove}
          className="px-3 py-1.5 rounded-xl bg-red-50"
          style={({ pressed }) => ({ opacity: pressed ? 0.7 : 1 })}
        >
          <Text className="text-xs font-semibold text-red-500">Remove</Text>
        </Pressable>
      )}
    </View>
  );
}

export default function FamilyScreen() {
  const router = useRouter();
  const [familyData, setFamilyData] = useState<FamilyWithMembers | null>(null);
  const [loading, setLoading] = useState(true);
  const [refreshing, setRefreshing] = useState(false);
  const [createName, setCreateName] = useState("");
  const [joinCode, setJoinCode] = useState("");
  const [submitting, setSubmitting] = useState(false);

  const fetchFamily = useCallback(async () => {
    try {
      const res = await getFamily();
      setFamilyData(res.data ?? null);
    } catch {
      // No family or error
      setFamilyData(null);
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    fetchFamily();
  }, [fetchFamily]);

  const onRefresh = async () => {
    setRefreshing(true);
    await fetchFamily();
    setRefreshing(false);
  };

  const handleCreate = async () => {
    if (!createName.trim()) return;
    setSubmitting(true);
    try {
      await createFamily(createName.trim());
      await fetchFamily();
      setCreateName("");
    } catch (err: any) {
      Alert.alert("Error", err.message ?? "Failed to create family");
    } finally {
      setSubmitting(false);
    }
  };

  const handleJoin = async () => {
    if (!joinCode.trim()) return;
    setSubmitting(true);
    try {
      await joinFamily(joinCode.trim());
      await fetchFamily();
      setJoinCode("");
    } catch (err: any) {
      Alert.alert("Error", err.message ?? "Failed to join family");
    } finally {
      setSubmitting(false);
    }
  };

  const handleRemoveMember = (member: FamilyMember) => {
    Alert.alert(
      "Remove Member",
      `Are you sure you want to remove this member?`,
      [
        { text: "Cancel", style: "cancel" },
        {
          text: "Remove",
          style: "destructive",
          onPress: async () => {
            try {
              await removeFamilyMember(member.familyId, member.userId);
              await fetchFamily();
            } catch (err: any) {
              Alert.alert("Error", err.message ?? "Failed to remove member");
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

  // User has no family — show create/join
  if (!familyData || !familyData.family) {
    return (
      <SafeAreaView className="flex-1 bg-companion-bg" edges={["bottom"]}>
        <ScrollView contentContainerStyle={{ padding: 16, paddingBottom: 48 }}>
          <View className="mb-6">
            <Text className="text-2xl font-bold text-warmgray-800">
              Family Accounts
            </Text>
            <Text className="text-sm text-warmgray-500 mt-1">
              Create or join a family to share emotional insights
            </Text>
          </View>

          {/* Create Family */}
          <View className="bg-white rounded-3xl p-5 border border-warmgray-100 mb-4">
            <Text className="text-lg font-bold text-warmgray-800 mb-3">
              Create a Family
            </Text>
            <TextInput
              className="border border-warmgray-200 rounded-xl px-4 py-3 text-base text-warmgray-800 mb-3"
              placeholder="Family name"
              placeholderTextColor="#A89885"
              value={createName}
              onChangeText={setCreateName}
            />
            <Pressable
              onPress={handleCreate}
              disabled={submitting || !createName.trim()}
              className="bg-primary-500 rounded-2xl py-3 items-center"
              style={({ pressed }) => ({
                opacity: pressed || submitting || !createName.trim() ? 0.6 : 1,
              })}
            >
              {submitting ? (
                <ActivityIndicator color="#fff" size="small" />
              ) : (
                <Text className="text-white font-bold text-base">
                  Create Family
                </Text>
              )}
            </Pressable>
          </View>

          {/* Join Family */}
          <View className="bg-white rounded-3xl p-5 border border-warmgray-100">
            <Text className="text-lg font-bold text-warmgray-800 mb-3">
              Join a Family
            </Text>
            <TextInput
              className="border border-warmgray-200 rounded-xl px-4 py-3 text-base text-warmgray-800 mb-3"
              placeholder="Invite code"
              placeholderTextColor="#A89885"
              value={joinCode}
              onChangeText={setJoinCode}
              autoCapitalize="characters"
              maxLength={8}
            />
            <Pressable
              onPress={handleJoin}
              disabled={submitting || !joinCode.trim()}
              className="bg-warmgray-700 rounded-2xl py-3 items-center"
              style={({ pressed }) => ({
                opacity: pressed || submitting || !joinCode.trim() ? 0.6 : 1,
              })}
            >
              {submitting ? (
                <ActivityIndicator color="#fff" size="small" />
              ) : (
                <Text className="text-white font-bold text-base">
                  Join Family
                </Text>
              )}
            </Pressable>
          </View>
        </ScrollView>
      </SafeAreaView>
    );
  }

  const { family, members } = familyData;
  const currentUserIsOwner = members.some((m) => m.role === "Owner");

  return (
    <SafeAreaView className="flex-1 bg-companion-bg" edges={["bottom"]}>
      <ScrollView
        contentContainerStyle={{ padding: 16, paddingBottom: 48 }}
        refreshControl={
          <RefreshControl refreshing={refreshing} onRefresh={onRefresh} />
        }
      >
        {/* Family Header */}
        <View className="bg-white rounded-3xl p-5 border border-warmgray-100 mb-4">
          <Text className="text-2xl font-bold text-warmgray-800">
            {family.name}
          </Text>
          <Text className="text-sm text-warmgray-500 mt-1">
            {members.length} member{members.length !== 1 ? "s" : ""}
          </Text>
        </View>

        {/* Action Buttons */}
        <View className="flex-row gap-3 mb-4">
          {currentUserIsOwner && (
            <Pressable
              onPress={() => router.push("/family/invite")}
              className="flex-1 bg-primary-500 rounded-2xl py-3 items-center"
              style={({ pressed }) => ({ opacity: pressed ? 0.8 : 1 })}
            >
              <Text className="text-white font-bold text-base">
                Invite Member
              </Text>
            </Pressable>
          )}
          <Pressable
            onPress={() => router.push(`/family/insights?familyId=${family.id}`)}
            className="flex-1 bg-warmgray-700 rounded-2xl py-3 items-center"
            style={({ pressed }) => ({ opacity: pressed ? 0.8 : 1 })}
          >
            <Text className="text-white font-bold text-base">
              View Insights
            </Text>
          </Pressable>
        </View>

        {/* Members List */}
        <Text className="text-lg font-bold text-warmgray-800 mb-3">
          Members
        </Text>
        <View className="gap-3">
          {members.map((member) => (
            <MemberCard
              key={member.id}
              member={member}
              isOwner={currentUserIsOwner}
              onRemove={() => handleRemoveMember(member)}
            />
          ))}
        </View>
      </ScrollView>
    </SafeAreaView>
  );
}
