import { ChatWindow } from "./components/ChatWindow";
import { ChatInput } from "./components/ChatInput";
import { useChat } from "./hooks/useChat";

function App() {
  const { messages, isLoading, error, sendMessage, clearChat } = useChat();

  return (
    <div className="app">
      <header className="app__header">
        <h1>AdventureWorks RAG</h1>
        {messages.length > 0 && (
          <button className="app__clear-btn" onClick={clearChat}>
            Clear
          </button>
        )}
      </header>

      <main className="app__main">
        <ChatWindow messages={messages} />
        {error && <p className="app__error">{error}</p>}
      </main>

      <footer className="app__footer">
        <ChatInput onSend={sendMessage} isLoading={isLoading} />
      </footer>
    </div>
  );
}

export default App;
