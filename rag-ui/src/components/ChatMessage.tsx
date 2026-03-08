import { SourceBadge } from "./SourceBadge";
import type { ChatMessage } from "../types/chat";

interface ChatMessageProps {
  message: ChatMessage;
}

export function ChatMessage({ message }: ChatMessageProps) {
  const isUser = message.role === "user";

  return (
    <div className={`chat-message ${isUser ? "chat-message--user" : "chat-message--assistant"}`}>
      <div className="chat-message__bubble">
        <p className="chat-message__content">{message.content}</p>
        {!isUser && message.sources && message.sources.length > 0 && (
          <div className="chat-message__sources">
            {message.sources.map((source) => (
              <SourceBadge key={source} source={source} />
            ))}
          </div>
        )}
      </div>
    </div>
  );
}
