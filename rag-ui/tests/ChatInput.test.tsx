import { describe, it, expect, vi } from "vitest";
import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { ChatInput } from "../src/components/ChatInput";

describe("ChatInput", () => {
  it("renders_inputAndButton — input and button are visible", () => {
    render(<ChatInput onSend={vi.fn()} isLoading={false} />);
    expect(screen.getByRole("textbox")).toBeInTheDocument();
    expect(screen.getByRole("button", { name: /send/i })).toBeInTheDocument();
  });

  it("button_disabled_whenLoading — Send button is disabled when isLoading=true", () => {
    render(<ChatInput onSend={vi.fn()} isLoading={true} />);
    expect(screen.getByRole("button", { name: /send/i })).toBeDisabled();
  });

  it("button_enabled_whenNotLoading — Send button is enabled when isLoading=false", () => {
    render(<ChatInput onSend={vi.fn()} isLoading={false} />);
    expect(screen.getByRole("button", { name: /send/i })).not.toBeDisabled();
  });

  it("calls_onSend_onButtonClick — onSend called with input value on click", async () => {
    const onSend = vi.fn();
    render(<ChatInput onSend={onSend} isLoading={false} />);

    await userEvent.type(screen.getByRole("textbox"), "Hello");
    await userEvent.click(screen.getByRole("button", { name: /send/i }));

    expect(onSend).toHaveBeenCalledWith("Hello");
  });

  it("calls_onSend_onEnterKey — onSend called on Enter key press", async () => {
    const onSend = vi.fn();
    render(<ChatInput onSend={onSend} isLoading={false} />);

    await userEvent.type(screen.getByRole("textbox"), "Hello{Enter}");

    expect(onSend).toHaveBeenCalledWith("Hello");
  });

  it("clears_input_afterSend — input field is empty after send", async () => {
    render(<ChatInput onSend={vi.fn()} isLoading={false} />);
    const input = screen.getByRole("textbox");

    await userEvent.type(input, "Hello");
    await userEvent.click(screen.getByRole("button", { name: /send/i }));

    expect(input).toHaveValue("");
  });

  it("does_not_call_onSend_whenEmpty — onSend NOT called with empty input", async () => {
    const onSend = vi.fn();
    render(<ChatInput onSend={onSend} isLoading={false} />);

    await userEvent.click(screen.getByRole("button", { name: /send/i }));

    expect(onSend).not.toHaveBeenCalled();
  });
});
