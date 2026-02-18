import React from "react";
import { View, useWindowDimensions } from "react-native";

interface ResponsiveContainerProps {
  children: React.ReactNode;
  maxWidth?: number;
}

export default function ResponsiveContainer({
  children,
  maxWidth = 768,
}: ResponsiveContainerProps) {
  const { width } = useWindowDimensions();

  if (width <= maxWidth) {
    return <>{children}</>;
  }

  return (
    <View className="flex-1 items-center">
      <View style={{ width: maxWidth, flex: 1 }}>{children}</View>
    </View>
  );
}
