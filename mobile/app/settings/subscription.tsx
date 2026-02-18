import React from "react";
import {
  View,
  Text,
  Pressable,
  ScrollView,
  ActivityIndicator,
  Alert,
  Platform,
  Linking,
} from "react-native";
import { SafeAreaView } from "react-native-safe-area-context";
import { useSubscription } from "@/lib/hooks";
import type { SubscriptionTierInfo, SubscriptionTier } from "@/lib/api";

const TIER_ACCENTS: Record<string, string> = {
  free: "#A89885",
  plus: "#FF8B47",
  premium: "#8B5CF6",
};

function TierCard({
  tier,
  isActive,
  onSelect,
  isCheckingOut,
}: {
  tier: SubscriptionTierInfo;
  isActive: boolean;
  onSelect: () => void;
  isCheckingOut: boolean;
}) {
  const accent = TIER_ACCENTS[tier.tier] ?? "#A89885";

  return (
    <View
      className={`bg-white rounded-3xl p-5 border-2 ${
        isActive ? "border-primary-500" : "border-warmgray-100"
      }`}
      style={isActive ? { borderColor: accent } : undefined}
    >
      <View className="flex-row items-center justify-between mb-2">
        <Text className="text-lg font-bold text-warmgray-800">{tier.name}</Text>
        {isActive && (
          <View className="bg-primary-50 rounded-full px-3 py-1">
            <Text className="text-xs font-semibold text-primary-600">
              Current Plan
            </Text>
          </View>
        )}
      </View>

      <View className="flex-row items-baseline mb-4">
        {tier.price === 0 ? (
          <Text className="text-2xl font-bold text-warmgray-800">Free</Text>
        ) : (
          <>
            <Text className="text-2xl font-bold text-warmgray-800">
              ${tier.price.toFixed(2)}
            </Text>
            <Text className="text-sm text-warmgray-400 ml-1">/month</Text>
          </>
        )}
      </View>

      <View className="gap-2 mb-4">
        {tier.features.map((feature, idx) => (
          <View key={idx} className="flex-row items-start gap-2">
            <Text style={{ color: accent }} className="text-sm mt-0.5">
              &#10003;
            </Text>
            <Text className="text-sm text-warmgray-600 flex-1">{feature}</Text>
          </View>
        ))}
      </View>

      {!isActive && tier.price > 0 && (
        <Pressable
          onPress={onSelect}
          disabled={isCheckingOut}
          className="rounded-2xl py-3 items-center"
          style={[
            { backgroundColor: accent },
            ({ pressed }) => ({ opacity: pressed ? 0.8 : 1 }),
          ] as any}
        >
          {isCheckingOut ? (
            <ActivityIndicator color="#fff" size="small" />
          ) : (
            <Text className="text-white font-bold text-base">
              Upgrade to {tier.name}
            </Text>
          )}
        </Pressable>
      )}
    </View>
  );
}

export default function SubscriptionScreen() {
  const {
    tiers,
    isLoadingTiers,
    subscription,
    isLoadingSubscription,
    createCheckout,
    isCheckingOut,
    cancelSubscription,
    isCanceling,
  } = useSubscription();

  const currentTier = subscription?.tier ?? "free";

  const handleSelectTier = async (tier: SubscriptionTier) => {
    try {
      const platform = Platform.OS === "web" ? "web" : "mobile";
      const result = await createCheckout({ tier, platform });

      if (Platform.OS === "web" && result.url) {
        // Redirect to Stripe Checkout on web
        Linking.openURL(result.url);
      } else if (Platform.OS !== "web" && result.clientSecret) {
        // Use Stripe Payment Sheet on native
        try {
          const { initPaymentSheet, presentPaymentSheet } = await import(
            "@stripe/stripe-react-native"
          );

          const { error: initError } = await initPaymentSheet({
            paymentIntentClientSecret: result.clientSecret,
            merchantDisplayName: "Digital Twin Companion",
          });

          if (initError) {
            throw new Error(initError.message);
          }

          const { error: presentError } = await presentPaymentSheet();

          if (presentError) {
            if (presentError.code !== "Canceled") {
              throw new Error(presentError.message);
            }
          } else {
            Alert.alert("Success", "Your subscription has been activated!");
          }
        } catch (stripeErr: any) {
          Alert.alert("Payment Error", stripeErr.message ?? "Payment failed. Please try again.");
        }
      }
    } catch (err: any) {
      const msg = err.message ?? "Failed to start checkout. Please try again.";
      if (Platform.OS === "web") window.alert(msg);
      else Alert.alert("Error", msg);
    }
  };

  const handleCancel = () => {
    const doCancel = async () => {
      try {
        await cancelSubscription();
        const msg = "Your subscription will remain active until the end of the billing period.";
        if (Platform.OS === "web") window.alert(msg);
        else Alert.alert("Subscription Canceled", msg);
      } catch (err: any) {
        const msg = err.message ?? "Failed to cancel. Please try again.";
        if (Platform.OS === "web") window.alert(msg);
        else Alert.alert("Error", msg);
      }
    };

    if (Platform.OS === "web") {
      if (window.confirm("Are you sure you want to cancel your subscription?")) {
        doCancel();
      }
    } else {
      Alert.alert(
        "Cancel Subscription",
        "Are you sure? You'll keep access until the end of your billing period.",
        [
          { text: "Keep Plan", style: "cancel" },
          { text: "Cancel Plan", style: "destructive", onPress: doCancel },
        ]
      );
    }
  };

  if (isLoadingTiers || isLoadingSubscription) {
    return (
      <SafeAreaView className="flex-1 bg-companion-bg items-center justify-center" edges={["bottom"]}>
        <ActivityIndicator size="large" color="#FF8B47" />
      </SafeAreaView>
    );
  }

  return (
    <SafeAreaView className="flex-1 bg-companion-bg" edges={["bottom"]}>
      <ScrollView contentContainerStyle={{ padding: 16, paddingBottom: 48 }}>
        {/* Active subscription info */}
        {subscription && subscription.tier !== "free" && (
          <View className="bg-white rounded-2xl p-4 border border-warmgray-100 mb-4">
            <Text className="text-sm font-semibold text-warmgray-800">
              Active Subscription
            </Text>
            {subscription.currentPeriodEnd && (
              <Text className="text-sm text-warmgray-400 mt-1">
                {subscription.cancelAtPeriodEnd ? "Cancels" : "Renews"} on{" "}
                {new Date(subscription.currentPeriodEnd).toLocaleDateString()}
              </Text>
            )}
            {subscription.cancelAtPeriodEnd && (
              <Text className="text-xs text-orange-500 mt-1">
                Will downgrade to Free at end of period
              </Text>
            )}
          </View>
        )}

        {/* Tier cards */}
        <View className="gap-4">
          {tiers.map((tier) => (
            <TierCard
              key={tier.tier}
              tier={tier}
              isActive={currentTier === tier.tier}
              onSelect={() => handleSelectTier(tier.tier as SubscriptionTier)}
              isCheckingOut={isCheckingOut}
            />
          ))}

          {tiers.length === 0 && (
            <>
              {/* Static fallback tiers if API unavailable */}
              <TierCard
                tier={{ tier: "free", name: "Free", price: 0, interval: "month", features: ["5 conversations per day", "Basic emotion detection", "Text chat only", "2D avatar"] }}
                isActive={currentTier === "free"}
                onSelect={() => {}}
                isCheckingOut={false}
              />
              <TierCard
                tier={{ tier: "plus", name: "Plus", price: 9.99, interval: "month", features: ["Unlimited conversations", "Advanced emotion detection", "Voice conversations", "3D avatar generation", "Voice cloning", "Priority support"] }}
                isActive={currentTier === "plus"}
                onSelect={() => handleSelectTier("plus")}
                isCheckingOut={isCheckingOut}
              />
              <TierCard
                tier={{ tier: "premium", name: "Premium", price: 19.99, interval: "month", features: ["Everything in Plus", "Camera emotion detection", "Custom personality tuning", "Conversation export", "Advanced analytics", "Early access to features", "Dedicated support"] }}
                isActive={currentTier === "premium"}
                onSelect={() => handleSelectTier("premium")}
                isCheckingOut={isCheckingOut}
              />
            </>
          )}
        </View>

        {/* Cancel button */}
        {subscription && subscription.tier !== "free" && !subscription.cancelAtPeriodEnd && (
          <Pressable
            onPress={handleCancel}
            disabled={isCanceling}
            className="mt-6 bg-white border border-red-200 rounded-2xl py-4 items-center"
            style={({ pressed }) => ({ opacity: pressed ? 0.7 : 1 })}
          >
            {isCanceling ? (
              <ActivityIndicator color="#ef4444" size="small" />
            ) : (
              <Text className="text-red-500 font-semibold text-base">
                Cancel Subscription
              </Text>
            )}
          </Pressable>
        )}
      </ScrollView>
    </SafeAreaView>
  );
}
