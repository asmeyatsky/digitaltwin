import { getEmotionColor, EMOTION_COLORS } from "@/lib/hooks";

describe("getEmotionColor", () => {
  test("returns correct color for known emotion", () => {
    expect(getEmotionColor("joy")).toBe("#FFD166");
    expect(getEmotionColor("sadness")).toBe("#7BA7C9");
    expect(getEmotionColor("anger")).toBe("#E07A7A");
    expect(getEmotionColor("calm")).toBe("#A8D8B9");
  });

  test("returns neutral for unknown emotion", () => {
    expect(getEmotionColor("unknown")).toBe(EMOTION_COLORS.neutral);
  });

  test("returns neutral for undefined", () => {
    expect(getEmotionColor(undefined)).toBe(EMOTION_COLORS.neutral);
  });

  test("is case insensitive", () => {
    expect(getEmotionColor("Joy")).toBe("#FFD166");
    expect(getEmotionColor("CALM")).toBe("#A8D8B9");
  });
});
