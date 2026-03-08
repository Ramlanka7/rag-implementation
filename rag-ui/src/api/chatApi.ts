import type { ChatResponse } from "../types/chat";

const BASE_URL = import.meta.env.VITE_API_URL ?? "";

export async function sendQuestion(question: string): Promise<ChatResponse> {
  const response = await fetch(`${BASE_URL}/api/chat`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ question }),
  });

  if (!response.ok) {
    throw new Error(`Request failed with status ${response.status}`);
  }

  return response.json() as Promise<ChatResponse>;
}
