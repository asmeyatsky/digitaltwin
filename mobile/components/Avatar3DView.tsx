import React, { Suspense, useRef, useMemo } from "react";
import { View, Platform } from "react-native";
import AvatarView from "./AvatarView";

interface Avatar3DViewProps {
  avatarUrl?: string | null;
  emotion?: string;
  size?: number;
  interactive?: boolean;
  initials?: string;
}

// Lazy 3D canvas — only loads on native when GL is available
function Avatar3DCanvas({
  avatarUrl,
  emotion,
  size,
  interactive,
}: {
  avatarUrl: string;
  emotion?: string;
  size: number;
  interactive?: boolean;
}) {
  // Dynamic imports for three.js to avoid SSR/web bundling issues
  const Canvas = require("@react-three/fiber/native").Canvas;
  const useGLTF = require("@react-three/drei").useGLTF;
  const OrbitControls = require("@react-three/drei").OrbitControls;
  const useFrame = require("@react-three/fiber").useFrame;

  function AvatarModel({ url, emotion: modelEmotion }: { url: string; emotion?: string }) {
    const { scene } = useGLTF(url);
    const ref = useRef<any>();

    // Breathing animation
    useFrame(({ clock }: { clock: any }) => {
      if (ref.current) {
        const t = clock.getElapsedTime();
        ref.current.position.y = Math.sin(t * 1.5) * 0.02;
        ref.current.rotation.y = Math.sin(t * 0.5) * 0.05;
      }
    });

    // Apply emotion-based blend shapes if available
    const emotionScale = useMemo(() => {
      switch (modelEmotion?.toLowerCase()) {
        case "joy":
        case "happiness":
        case "happy":
          return { y: 1.02, scaleMultiplier: 1.01 };
        case "sadness":
        case "sad":
          return { y: 0.98, scaleMultiplier: 0.99 };
        default:
          return { y: 1, scaleMultiplier: 1 };
      }
    }, [modelEmotion]);

    return (
      <group ref={ref} scale={[emotionScale.scaleMultiplier, emotionScale.scaleMultiplier, emotionScale.scaleMultiplier]}>
        <primitive object={scene} scale={2} position={[0, -1, 0]} />
      </group>
    );
  }

  return (
    <View style={{ width: size, height: size }}>
      <Canvas
        style={{ width: size, height: size }}
        camera={{ position: [0, 0, 3], fov: 40 }}
      >
        <ambientLight intensity={0.6} />
        <directionalLight position={[5, 5, 5]} intensity={0.8} />
        <Suspense fallback={null}>
          <AvatarModel url={avatarUrl} emotion={emotion} />
        </Suspense>
        {interactive && <OrbitControls enableZoom={false} enablePan={false} />}
      </Canvas>
    </View>
  );
}

export default function Avatar3DView({
  avatarUrl,
  emotion,
  size = 64,
  interactive = false,
  initials = "DT",
}: Avatar3DViewProps) {
  // Fallback to 2D for web or when no GLB URL
  if (Platform.OS === "web" || !avatarUrl || !avatarUrl.endsWith(".glb")) {
    return (
      <AvatarView
        avatarUrl={avatarUrl?.endsWith(".glb") ? undefined : avatarUrl}
        emotion={emotion}
        size={size}
        initials={initials}
      />
    );
  }

  // Try 3D rendering, fallback to 2D on error
  try {
    return (
      <Avatar3DCanvas
        avatarUrl={avatarUrl}
        emotion={emotion}
        size={size}
        interactive={interactive}
      />
    );
  } catch {
    return (
      <AvatarView
        emotion={emotion}
        size={size}
        initials={initials}
      />
    );
  }
}
