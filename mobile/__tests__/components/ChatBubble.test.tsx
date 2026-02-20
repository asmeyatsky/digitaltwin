import React from "react";
import { render } from "@testing-library/react-native";
import ChatBubble from "@/components/ChatBubble";

// Mock hooks
jest.mock("@/lib/hooks", () => ({
  getEmotionColor: jest.fn(() => "#FFD166"),
}));

// Mock AudioPlayButton
jest.mock("@/components/AudioPlayButton", () => {
  const { Text } = require("react-native");
  return function MockAudioPlayButton() {
    return <Text>AudioPlay</Text>;
  };
});

const baseTimestamp = "2024-06-15T10:30:00Z";

describe("ChatBubble", () => {
  test("renders user message content", () => {
    const message = {
      id: "m1",
      conversationId: "c1",
      role: "user" as const,
      content: "Hello there!",
      timestamp: baseTimestamp,
    };
    const { getByText } = render(<ChatBubble message={message} />);
    expect(getByText("Hello there!")).toBeTruthy();
  });

  test("renders assistant message content", () => {
    const message = {
      id: "m2",
      conversationId: "c1",
      role: "assistant" as const,
      content: "How can I help?",
      timestamp: baseTimestamp,
    };
    const { getByText } = render(<ChatBubble message={message} />);
    expect(getByText("How can I help?")).toBeTruthy();
  });

  test("renders timestamp", () => {
    const message = {
      id: "m1",
      conversationId: "c1",
      role: "user" as const,
      content: "Hi",
      timestamp: baseTimestamp,
    };
    const { getByText } = render(<ChatBubble message={message} />);
    // toLocaleTimeString will vary by locale, just check it renders something
    const timeElements = new Date(baseTimestamp).toLocaleTimeString([], {
      hour: "2-digit",
      minute: "2-digit",
    });
    expect(getByText(timeElements)).toBeTruthy();
  });

  test("renders emotion badge for assistant", () => {
    const message = {
      id: "m2",
      conversationId: "c1",
      role: "assistant" as const,
      content: "I understand",
      timestamp: baseTimestamp,
      emotion: { primary: "calm", confidence: 0.8, valence: 0.5, arousal: 0.2 },
    };
    const { getByText } = render(<ChatBubble message={message} />);
    expect(getByText("calm")).toBeTruthy();
  });

  test("hides emotion for user messages", () => {
    const message = {
      id: "m1",
      conversationId: "c1",
      role: "user" as const,
      content: "Hi",
      timestamp: baseTimestamp,
      emotion: { primary: "happy", confidence: 0.9, valence: 0.8, arousal: 0.7 },
    };
    const { queryByText } = render(<ChatBubble message={message} />);
    // The emotion badge text should NOT appear for user messages
    // (the content "Hi" should, but not the emotion badge label)
    // ChatBubble only shows emotion when !isUser
    expect(queryByText("happy")).toBeNull();
  });
});
