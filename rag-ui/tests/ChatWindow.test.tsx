import { describe, it, expect } from "vitest";
import { render, screen } from "@testing-library/react";
import { ChatWindow } from "../src/components/ChatWindow";
import type { ChatMessage } from "../src/types/chat";

describe("ChatWindow", () => {
  it("renders_emptyState — empty container renders without error", () => {
    const { container } = render(<ChatWindow messages={[]} />);
    expect(container.querySelector(".chat-window")).toBeInTheDocument();
  });

  it("renders_userMessage — user message content is visible", () => {
    const messages: ChatMessage[] = [
      { role: "user", content: "What is the top product?" },
    ];
    render(<ChatWindow messages={messages} />);
    expect(screen.getByText("What is the top product?")).toBeInTheDocument();
  });

  it("renders_assistantMessage — assistant message content is visible", () => {
    const messages: ChatMessage[] = [
      { role: "assistant", content: "The Mountain Bike was top." },
    ];
    render(<ChatWindow messages={messages} />);
    expect(screen.getByText("The Mountain Bike was top.")).toBeInTheDocument();
  });

  it("renders_sources_underAssistantMessage — source badge IDs are visible", () => {
    const messages: ChatMessage[] = [
      {
        role: "assistant",
        content: "Here is the answer.",
        sources: ["product-772", "orderdetail-1045"],
      },
    ];
    render(<ChatWindow messages={messages} />);
    expect(screen.getByText("product-772")).toBeInTheDocument();
    expect(screen.getByText("orderdetail-1045")).toBeInTheDocument();
  });

  it("renders_multiple_messages_inOrder — all messages appear in correct order", () => {
    const messages: ChatMessage[] = [
      { role: "user", content: "First message" },
      { role: "assistant", content: "Second message" },
      { role: "user", content: "Third message" },
    ];
    render(<ChatWindow messages={messages} />);

    const contents = screen
      .getAllByText(/First message|Second message|Third message/)
      .map((el) => el.textContent);

    expect(contents).toEqual(["First message", "Second message", "Third message"]);
  });
});
