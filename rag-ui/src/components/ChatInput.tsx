import { useState, type KeyboardEvent } from "react";

interface ChatInputProps {
  onSend: (question: string) => void;
  isLoading: boolean;
}

export function ChatInput({ onSend, isLoading }: ChatInputProps) {
  const [value, setValue] = useState("");

  const handleSend = () => {
    const trimmed = value.trim();
    if (!trimmed) return;
    onSend(trimmed);
    setValue("");
  };

  const handleKeyDown = (e: KeyboardEvent<HTMLInputElement>) => {
    if (e.key === "Enter") {
      handleSend();
    }
  };

  return (
    <div className="chat-input">
      <input
        type="text"
        className="chat-input__field"
        placeholder="Ask a question about AdventureWorks..."
        value={value}
        onChange={(e) => setValue(e.target.value)}
        onKeyDown={handleKeyDown}
        disabled={isLoading}
        aria-label="Question input"
      />
      <button
        className="chat-input__button"
        onClick={handleSend}
        disabled={isLoading}
        aria-label="Send"
      >
        {isLoading ? "..." : "Send"}
      </button>
    </div>
  );
}
