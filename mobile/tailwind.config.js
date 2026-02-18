/** @type {import('tailwindcss').Config} */
module.exports = {
  content: [
    "./app/**/*.{ts,tsx}",
    "./components/**/*.{ts,tsx}",
    "./lib/**/*.{ts,tsx}",
  ],
  presets: [require("nativewind/preset")],
  theme: {
    extend: {
      colors: {
        primary: {
          50: "#FFF5EB",
          100: "#FFE8D1",
          200: "#FFD1A3",
          300: "#FFB975",
          400: "#FFA247",
          500: "#FF8B19",
          600: "#E07000",
          700: "#A85400",
          800: "#703800",
          900: "#381C00",
        },
        warmgray: {
          50: "#FAF8F5",
          100: "#F5F0EB",
          200: "#EBE1D6",
          300: "#DDD0C0",
          400: "#C7B8A5",
          500: "#A89885",
          600: "#8A7B69",
          700: "#6B5E4F",
          800: "#4D4235",
          900: "#2E271F",
        },
        companion: {
          bg: "#FDF8F3",
          card: "#FFFFFF",
          accent: "#FF8B47",
          warm: "#FFB088",
          cool: "#8BBFDE",
          joy: "#FFD166",
          calm: "#A8D8B9",
          sadness: "#7BA7C9",
          anger: "#E07A7A",
          surprise: "#C5A3E0",
          fear: "#8C8CBF",
          love: "#F5A0B8",
        },
      },
      borderRadius: {
        "2xl": "1rem",
        "3xl": "1.5rem",
        "4xl": "2rem",
      },
      fontFamily: {
        sans: ["System"],
      },
    },
  },
  plugins: [],
};
