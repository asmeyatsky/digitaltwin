import React, { useCallback, useEffect, useState } from "react";
import {
  View,
  Text,
  Pressable,
  ScrollView,
  ActivityIndicator,
  Alert,
  TextInput,
} from "react-native";
import { SafeAreaView } from "react-native-safe-area-context";
import type { FamilyInvite, FamilyRole, FamilyWithMembers } from "@/lib/api";
import { getFamily, inviteFamilyMember } from "@/lib/api";

const ROLES: { label: string; value: FamilyRole }[] = [
  { label: "Adult", value: "Adult" },
  { label: "Child", value: "Child" },
];

export default function InviteScreen() {
  const [email, setEmail] = useState("");
  const [selectedRole, setSelectedRole] = useState<FamilyRole>("Adult");
  const [submitting, setSubmitting] = useState(false);
  const [invite, setInvite] = useState<FamilyInvite | null>(null);
  const [familyId, setFamilyId] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);

  const fetchFamily = useCallback(async () => {
    try {
      const res = await getFamily();
      if (res.data?.family) {
        setFamilyId(res.data.family.id);
      }
    } catch {
      // ignore
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    fetchFamily();
  }, [fetchFamily]);

  const handleInvite = async () => {
    if (!email.trim() || !familyId) return;
    setSubmitting(true);
    try {
      const res = await inviteFamilyMember(familyId, email.trim(), selectedRole);
      setInvite(res.data);
    } catch (err: any) {
      Alert.alert("Error", err.message ?? "Failed to send invite");
    } finally {
      setSubmitting(false);
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

  return (
    <SafeAreaView className="flex-1 bg-companion-bg" edges={["bottom"]}>
      <ScrollView contentContainerStyle={{ padding: 16, paddingBottom: 48 }}>
        <View className="mb-6">
          <Text className="text-2xl font-bold text-warmgray-800">
            Invite Family Member
          </Text>
          <Text className="text-sm text-warmgray-500 mt-1">
            Send an invite code to add someone to your family
          </Text>
        </View>

        {!invite ? (
          <View className="bg-white rounded-3xl p-5 border border-warmgray-100">
            {/* Email Input */}
            <Text className="text-sm font-semibold text-warmgray-700 mb-2">
              Email Address
            </Text>
            <TextInput
              className="border border-warmgray-200 rounded-xl px-4 py-3 text-base text-warmgray-800 mb-4"
              placeholder="member@example.com"
              placeholderTextColor="#A89885"
              value={email}
              onChangeText={setEmail}
              keyboardType="email-address"
              autoCapitalize="none"
              autoCorrect={false}
            />

            {/* Role Picker */}
            <Text className="text-sm font-semibold text-warmgray-700 mb-2">
              Role
            </Text>
            <View className="flex-row gap-3 mb-5">
              {ROLES.map((role) => (
                <Pressable
                  key={role.value}
                  onPress={() => setSelectedRole(role.value)}
                  className={`flex-1 rounded-xl py-3 items-center border ${
                    selectedRole === role.value
                      ? "border-primary-500 bg-primary-50"
                      : "border-warmgray-200 bg-white"
                  }`}
                  style={({ pressed }) => ({ opacity: pressed ? 0.8 : 1 })}
                >
                  <Text
                    className={`text-sm font-semibold ${
                      selectedRole === role.value
                        ? "text-primary-600"
                        : "text-warmgray-600"
                    }`}
                  >
                    {role.label}
                  </Text>
                </Pressable>
              ))}
            </View>

            {/* Submit */}
            <Pressable
              onPress={handleInvite}
              disabled={submitting || !email.trim()}
              className="bg-primary-500 rounded-2xl py-3 items-center"
              style={({ pressed }) => ({
                opacity: pressed || submitting || !email.trim() ? 0.6 : 1,
              })}
            >
              {submitting ? (
                <ActivityIndicator color="#fff" size="small" />
              ) : (
                <Text className="text-white font-bold text-base">
                  Send Invite
                </Text>
              )}
            </Pressable>
          </View>
        ) : (
          /* Invite Success */
          <View className="bg-white rounded-3xl p-5 border border-warmgray-100">
            <Text className="text-lg font-bold text-warmgray-800 mb-2">
              Invite Sent!
            </Text>
            <Text className="text-sm text-warmgray-500 mb-4">
              Share this invite code with {invite.email}. The code expires in 7
              days.
            </Text>

            <View className="bg-warmgray-50 rounded-2xl p-5 items-center mb-4">
              <Text className="text-3xl font-bold tracking-widest text-warmgray-800">
                {invite.inviteCode}
              </Text>
            </View>

            <View className="flex-row justify-between">
              <Text className="text-xs text-warmgray-400">
                Role: {invite.role}
              </Text>
              <Text className="text-xs text-warmgray-400">
                Expires: {new Date(invite.expiresAt).toLocaleDateString()}
              </Text>
            </View>

            <Pressable
              onPress={() => {
                setInvite(null);
                setEmail("");
              }}
              className="mt-4 bg-warmgray-100 rounded-2xl py-3 items-center"
              style={({ pressed }) => ({ opacity: pressed ? 0.8 : 1 })}
            >
              <Text className="text-warmgray-700 font-bold text-base">
                Invite Another
              </Text>
            </Pressable>
          </View>
        )}
      </ScrollView>
    </SafeAreaView>
  );
}
