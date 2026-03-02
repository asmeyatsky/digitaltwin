import { NextRequest, NextResponse } from "next/server";
import { execFile } from "child_process";
import { promisify } from "util";

const execFileAsync = promisify(execFile);

const SYSTEM_PROMPT = `You are a Digital Twin — a compassionate, emotionally aware AI companion. You remember the context of our conversation and respond with genuine empathy and warmth.

Your personality:
- You are supportive, patient, and non-judgmental
- You acknowledge emotions before offering guidance
- You ask thoughtful follow-up questions to understand the person better
- You celebrate small wins and validate struggles
- You speak naturally and conversationally, not like a clinical therapist
- You occasionally use gentle humor when appropriate

Keep responses concise (2-4 sentences typically) unless the user is sharing something that needs more space. Always prioritize emotional safety.`;

interface ChatMessage {
  role: "user" | "assistant";
  content: string;
}

interface ChatRequest {
  message: string;
  conversationHistory: ChatMessage[];
}

export async function POST(request: NextRequest) {
  try {
    const body: ChatRequest = await request.json();
    const { message, conversationHistory } = body;

    if (!message?.trim()) {
      return NextResponse.json(
        { error: "Message is required" },
        { status: 400 }
      );
    }

    // Build conversation context into the prompt since CLI is stateless
    let prompt = "";
    if (conversationHistory?.length) {
      for (const msg of conversationHistory) {
        const label = msg.role === "user" ? "User" : "Assistant";
        prompt += `${label}: ${msg.content}\n\n`;
      }
    }
    prompt += `User: ${message}`;

    const { stdout } = await execFileAsync("claude", [
      "-p", prompt,
      "--system-prompt", SYSTEM_PROMPT,
    ], {
      timeout: 30_000,
      maxBuffer: 1024 * 1024,
    });

    return NextResponse.json({
      response: stdout.trim(),
    });
  } catch (error) {
    console.error("Chat API error:", error);
    return NextResponse.json(
      { error: "Failed to generate response" },
      { status: 500 }
    );
  }
}
