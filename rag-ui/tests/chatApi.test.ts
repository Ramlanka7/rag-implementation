import { describe, it, expect, vi, beforeEach } from "vitest";
import { sendQuestion } from "../src/api/chatApi";

const mockResponse = { answer: "Mountain Biked sold the most.", sources: ["product-772"] };

beforeEach(() => {
  vi.restoreAllMocks();
});

describe("sendQuestion", () => {
  it("sendQuestion_returnsAnswer — resolves with ChatResponse on 200", async () => {
    global.fetch = vi.fn().mockResolvedValue({
      ok: true,
      json: async () => mockResponse,
    } as Response);

    const result = await sendQuestion("What sold the most?");
    expect(result).toEqual(mockResponse);
  });

  it("sendQuestion_throwsOnNonOk — rejects on 500", async () => {
    global.fetch = vi.fn().mockResolvedValue({
      ok: false,
      status: 500,
    } as Response);

    await expect(sendQuestion("test")).rejects.toThrow("500");
  });

  it("sendQuestion_throwsOnNetworkError — rejects when fetch throws", async () => {
    global.fetch = vi.fn().mockRejectedValue(new Error("Network error"));

    await expect(sendQuestion("test")).rejects.toThrow("Network error");
  });

  it("sendQuestion_sendsCorrectBody — body JSON contains question field", async () => {
    global.fetch = vi.fn().mockResolvedValue({
      ok: true,
      json: async () => mockResponse,
    } as Response);

    await sendQuestion("How many sales?");

    const [, init] = (global.fetch as ReturnType<typeof vi.fn>).mock.calls[0];
    const body = JSON.parse(init.body as string);
    expect(body).toHaveProperty("question", "How many sales?");
  });
});
