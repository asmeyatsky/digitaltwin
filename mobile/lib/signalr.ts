import * as signalR from "@microsoft/signalr";
import { useAuthStore } from "./store";

const API_BASE =
  process.env.EXPO_PUBLIC_API_URL || "http://localhost:8080";

let connection: signalR.HubConnection | null = null;

export function getCompanionConnection(): signalR.HubConnection {
  if (connection) return connection;

  connection = new signalR.HubConnectionBuilder()
    .withUrl(`${API_BASE}/hubs/companion`, {
      accessTokenFactory: () => useAuthStore.getState().token ?? "",
    })
    .withAutomaticReconnect()
    .configureLogging(signalR.LogLevel.Information)
    .build();

  return connection;
}

export async function startConnection(): Promise<void> {
  const conn = getCompanionConnection();
  if (conn.state === signalR.HubConnectionState.Disconnected) {
    await conn.start();
  }
}

export async function stopConnection(): Promise<void> {
  if (connection && connection.state !== signalR.HubConnectionState.Disconnected) {
    await connection.stop();
  }
}

export async function joinRoom(roomId: string): Promise<void> {
  const conn = getCompanionConnection();
  await conn.invoke("JoinRoom", roomId);
}

export async function leaveRoom(roomId: string): Promise<void> {
  const conn = getCompanionConnection();
  await conn.invoke("LeaveRoom", roomId);
}

export async function sendRoomMessage(
  roomId: string,
  message: string
): Promise<void> {
  const conn = getCompanionConnection();
  await conn.invoke("SendMessage", roomId, message);
}

export async function shareEmotion(
  roomId: string,
  emotion: string,
  confidence: number
): Promise<void> {
  const conn = getCompanionConnection();
  await conn.invoke("ShareEmotion", roomId, emotion, confidence);
}

export async function syncAvatar(
  roomId: string,
  avatarState: Record<string, unknown>
): Promise<void> {
  const conn = getCompanionConnection();
  await conn.invoke("SyncAvatar", roomId, avatarState);
}

export type CompanionEventHandlers = {
  onUserJoined?: (data: { userId: string; roomId: string; timestamp?: string }) => void;
  onUserLeft?: (data: { userId: string; roomId: string; timestamp?: string }) => void;
  onReceiveMessage?: (data: {
    userId: string;
    roomId: string;
    message: string;
    timestamp?: string;
  }) => void;
  onEmotionShared?: (data: {
    userId: string;
    roomId: string;
    emotion: string;
    confidence: number;
    timestamp?: string;
  }) => void;
  onAvatarSync?: (data: {
    userId: string;
    roomId: string;
    avatarState: Record<string, unknown>;
    timestamp?: string;
  }) => void;
  onError?: (data: { message: string; code?: string; timestamp?: string }) => void;
};

export function registerHandlers(handlers: CompanionEventHandlers): void {
  const conn = getCompanionConnection();

  if (handlers.onUserJoined) conn.on("UserJoined", handlers.onUserJoined);
  if (handlers.onUserLeft) conn.on("UserLeft", handlers.onUserLeft);
  if (handlers.onReceiveMessage) conn.on("ReceiveMessage", handlers.onReceiveMessage);
  if (handlers.onEmotionShared) conn.on("EmotionShared", handlers.onEmotionShared);
  if (handlers.onAvatarSync) conn.on("AvatarSync", handlers.onAvatarSync);
  if (handlers.onError) conn.on("Error", handlers.onError);
}

export function unregisterHandlers(): void {
  const conn = getCompanionConnection();
  conn.off("UserJoined");
  conn.off("UserLeft");
  conn.off("ReceiveMessage");
  conn.off("EmotionShared");
  conn.off("AvatarSync");
  conn.off("Error");
}
