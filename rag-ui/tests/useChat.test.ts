import { describe, it, expect, vi, beforeEach } from "vitest";
import { renderHook, act } from "@testing-library/react";
import { useChat } from "../src/hooks/useChat";
import * as chatApi from "../src/api/chatApi";

const mockApiResponse = { answer: "Great answer.", sources: ["product-1"] };

beforeEach(() => {
  vi.restoreAllMocks();
});

describe("useChat", () => {
  it("initialState_isEmpty — messages empty, isLoading false", () => {
    const { result } = renderHook(() => useChat());
    expect(result.current.messages).toHaveLength(0);
    expect(result.current.isLoading).toBe(false);
    expect(result.current.error).toBeNull();
  });

  it("sendMessage_addsUserMessage — user message added to messages", async () => {
    vi.spyOn(chatApi, "sendQuestion").mockResolvedValue(mockApiResponse);
    const { result } = renderHook(() => useChat());

    await act(async () => {
      await result.current.sendMessage("Hello");
    });

    expect(result.current.messages[0]).toMatchObject({ role: "user", content: "Hello" });
  });

  it("sendMessage_addsAssistantMessage — assistant message added after user message", async () => {
    vi.spyOn(chatApi, "sendQuestion").mockResolvedValue(mockApiResponse);
    const { result } = renderHook(() => useChat());

    await act(async () => {
      await result.current.sendMessage("Hello");
    });

    expect(result.current.messages[1]).toMatchObject({
      role: "assistant",
      content: mockApiResponse.answer,
      sources: mockApiResponse.sources,
    });
  });

  it("sendMessage_setsLoadingTrue_duringRequest — isLoading true while in flight", async () => {
    let resolveApi!: (v: typeof mockApiResponse) => void;
    vi.spyOn(chatApi, "sendQuestion").mockReturnValue(
      new Promise((res) => { resolveApi = res; })
    );
    const { result } = renderHook(() => useChat());

    act(() => { result.current.sendMessage("test"); });
    expect(result.current.isLoading).toBe(true);

    await act(async () => { resolveApi(mockApiResponse); });
  });

  it("sendMessage_setsLoadingFalse_afterResponse — isLoading false after completion", async () => {
    vi.spyOn(chatApi, "sendQuestion").mockResolvedValue(mockApiResponse);
    const { result } = renderHook(() => useChat());

    await act(async () => {
      await result.current.sendMessage("Hello");
    });

    expect(result.current.isLoading).toBe(false);
  });

  it("sendMessage_setsError_onFailure — error is non-null on API rejection", async () => {
    vi.spyOn(chatApi, "sendQuestion").mockRejectedValue(new Error("API error"));
    const { result } = renderHook(() => useChat());

    await act(async () => {
      await result.current.sendMessage("Hello");
    });

    expect(result.current.error).toBe("API error");
  });

  it("clearChat_resetsMessages — messages array is empty after clear", async () => {
    vi.spyOn(chatApi, "sendQuestion").mockResolvedValue(mockApiResponse);
    const { result } = renderHook(() => useChat());

    await act(async () => {
      await result.current.sendMessage("Hello");
    });

    act(() => { result.current.clearChat(); });

    expect(result.current.messages).toHaveLength(0);
  });
});
