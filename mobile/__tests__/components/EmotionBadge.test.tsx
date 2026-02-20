import React from "react";
import { render } from "@testing-library/react-native";
import EmotionBadge from "@/components/EmotionBadge";

// Mock hooks
jest.mock("@/lib/hooks", () => ({
  getEmotionColor: jest.fn(() => "#FFD166"),
}));

describe("EmotionBadge", () => {
  test("renders null when emotion is null", () => {
    const { toJSON } = render(<EmotionBadge emotion={null} />);
    expect(toJSON()).toBeNull();
  });

  test("renders primary text", () => {
    const emotion = { primary: "joy", confidence: 0.9, valence: 0.8, arousal: 0.6 };
    const { getByText } = render(<EmotionBadge emotion={emotion} />);
    expect(getByText("joy")).toBeTruthy();
  });

  test("renders confidence percentage for md size", () => {
    const emotion = { primary: "joy", confidence: 0.85, valence: 0.8, arousal: 0.6 };
    const { getByText } = render(<EmotionBadge emotion={emotion} size="md" />);
    expect(getByText("85%")).toBeTruthy();
  });

  test("hides confidence for sm size", () => {
    const emotion = { primary: "joy", confidence: 0.85, valence: 0.8, arousal: 0.6 };
    const { queryByText } = render(<EmotionBadge emotion={emotion} size="sm" />);
    expect(queryByText("85%")).toBeNull();
  });

  test("renders correct emoji", () => {
    const emotion = { primary: "sadness", confidence: 0.7, valence: -0.5, arousal: 0.3 };
    const { getByText } = render(<EmotionBadge emotion={emotion} />);
    expect(getByText("😢")).toBeTruthy();
  });
});
