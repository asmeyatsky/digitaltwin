import React, { useRef, useState } from "react";
import {
  View,
  Text,
  ScrollView,
  Pressable,
  Dimensions,
  NativeSyntheticEvent,
  NativeScrollEvent,
} from "react-native";
import { SafeAreaView } from "react-native-safe-area-context";
import { useRouter } from "expo-router";

const { width: SCREEN_WIDTH } = Dimensions.get("window");

interface Slide {
  emoji: string;
  title: string;
  description: string;
}

const slides: Slide[] = [
  {
    emoji: "\uD83E\uDD1D",
    title: "Meet Your Companion",
    description: "An AI that truly understands how you feel",
  },
  {
    emoji: "\uD83C\uDF10",
    title: "Multi-Modal Understanding",
    description:
      "Voice, face, text, and biometrics \u2014 all working together",
  },
  {
    emoji: "\uD83C\uDF31",
    title: "Grow Together",
    description:
      "Track your emotions, set goals, and build resilience",
  },
];

export default function WelcomeScreen() {
  const router = useRouter();
  const scrollRef = useRef<ScrollView>(null);
  const [activeIndex, setActiveIndex] = useState(0);

  const handleScroll = (e: NativeSyntheticEvent<NativeScrollEvent>) => {
    const offsetX = e.nativeEvent.contentOffset.x;
    const index = Math.round(offsetX / SCREEN_WIDTH);
    setActiveIndex(index);
  };

  const goToMoodCheck = () => {
    router.push("/onboarding/mood-check");
  };

  return (
    <SafeAreaView className="flex-1 bg-companion-bg">
      {/* Skip button */}
      <View className="flex-row justify-end px-6 pt-4">
        <Pressable
          onPress={goToMoodCheck}
          style={({ pressed }) => ({ opacity: pressed ? 0.6 : 1 })}
        >
          <Text className="text-base text-warmgray-400 font-medium">
            Skip
          </Text>
        </Pressable>
      </View>

      {/* Slides */}
      <ScrollView
        ref={scrollRef}
        horizontal
        pagingEnabled
        showsHorizontalScrollIndicator={false}
        onScroll={handleScroll}
        scrollEventThrottle={16}
        className="flex-1"
      >
        {slides.map((slide, index) => (
          <View
            key={index}
            style={{ width: SCREEN_WIDTH }}
            className="flex-1 items-center justify-center px-10"
          >
            {/* Illustration area */}
            <View className="w-40 h-40 rounded-full bg-primary-50 items-center justify-center mb-10">
              <Text style={{ fontSize: 64 }}>{slide.emoji}</Text>
            </View>

            <Text className="text-3xl font-bold text-warmgray-800 text-center mb-4">
              {slide.title}
            </Text>
            <Text className="text-lg text-warmgray-500 text-center leading-7">
              {slide.description}
            </Text>
          </View>
        ))}
      </ScrollView>

      {/* Bottom section: dots + button */}
      <View className="items-center pb-8 px-6">
        {/* Dot indicators */}
        <View className="flex-row gap-2 mb-8">
          {slides.map((_, index) => (
            <View
              key={index}
              className={`h-2 rounded-full ${
                index === activeIndex
                  ? "w-8 bg-primary-500"
                  : "w-2 bg-warmgray-300"
              }`}
            />
          ))}
        </View>

        {/* Get Started / Next button */}
        {activeIndex === slides.length - 1 ? (
          <Pressable
            onPress={goToMoodCheck}
            className="w-full bg-primary-500 rounded-2xl py-4 items-center"
            style={({ pressed }) => ({ opacity: pressed ? 0.85 : 1 })}
          >
            <Text className="text-white text-base font-semibold">
              Get Started
            </Text>
          </Pressable>
        ) : (
          <Pressable
            onPress={() => {
              scrollRef.current?.scrollTo({
                x: (activeIndex + 1) * SCREEN_WIDTH,
                animated: true,
              });
            }}
            className="w-full bg-primary-500 rounded-2xl py-4 items-center"
            style={({ pressed }) => ({ opacity: pressed ? 0.85 : 1 })}
          >
            <Text className="text-white text-base font-semibold">
              Next
            </Text>
          </Pressable>
        )}
      </View>
    </SafeAreaView>
  );
}
