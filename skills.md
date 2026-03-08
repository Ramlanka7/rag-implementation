# AdventureWorks RAG — Implementation Skills Guide

This document is the step-by-step implementation plan for **rag-api** and **rag-ui**.
`rag-indexer` is already complete (Steps 1–2 done).

---

## Progress Tracker

| Step | Component   | Status      |
|------|-------------|-------------|
| 1    | Azure Infrastructure | ✅ Done |
| 2    | rag-indexer | ✅ Done |
| 3    | rag-api     | ✅ Done |
| 4    | rag-ui      | ✅ Done |

---

---

# Step 3 — rag-api (.NET 10 ASP.NET Core)

## Goal

Expose a `POST /api/chat` endpoint that runs the full RAG pipeline and returns an AI answer.

## RAG Pipeline (per request)

```
Incoming question
      │
EmbeddingService      → text-embedding-3-small → float[]
      │
VectorSearchService   → Azure AI Search        → top 5 documents
      │
PromptBuilder         → system prompt + context + question
      │
ChatService           → gpt-4o                 → answer string
      │
JSON response to caller
```

---

## Folder Structure

```
rag-api/
├── rag-api.csproj
├── appsettings.json
├── Program.cs
├── Controllers/
│   └── ChatController.cs
├── Models/
│   ├── ChatRequest.cs
│   └── ChatResponse.cs
├── Services/
│   ├── EmbeddingService.cs        ← same role as in rag-indexer
│   ├── VectorSearchService.cs     ← queries Azure AI Search
│   ├── PromptBuilder.cs           ← assembles system + context + user message
│   └── ChatService.cs             ← calls gpt-4o, returns answer
└── Tests/
    └── rag-api.Tests/
        ├── rag-api.Tests.csproj
        ├── ChatControllerTests.cs
        ├── VectorSearchServiceTests.cs
        ├── PromptBuilderTests.cs
        └── ChatServiceTests.cs
```

---

## Files to Create

### 1. `rag-api.csproj`

- SDK: `Microsoft.NET.Sdk.Web`
- Target: `net10.0`
- Packages:
  - `Azure.AI.OpenAI` 2.1.0
  - `Azure.Search.Documents` 11.6.0

---

### 2. `appsettings.json`

```json
{
  "AzureOpenAI": {
    "Endpoint": "<your-endpoint>",
    "ApiKey": "<your-api-key>",
    "EmbeddingDeployment": "text-embedding-3-small",
    "ChatDeployment": "gpt-4o"
  },
  "AzureSearch": {
    "Endpoint": "<your-search-endpoint>",
    "ApiKey": "<your-admin-key>",
    "IndexName": "adventureworks-index",
    "TopK": 5
  }
}
```

---

### 3. `Models/ChatRequest.cs`

```
{
  "question": "Which products sold the most in 2013?"
}
```

- `Question` — string, required, max 1000 chars

---

### 4. `Models/ChatResponse.cs`

```
{
  "answer": "The Mountain Bike was the top-selling product in 2013...",
  "sources": ["product-772", "orderdetail-1045"]
}
```

- `Answer` — string
- `Sources` — list of document IDs retrieved from search

---

### 5. `Services/EmbeddingService.cs`

Same as rag-indexer. Converts a single question string into a `float[]` vector using `text-embedding-3-small`.

Method:
```csharp
Task<float[]> GenerateEmbeddingAsync(string text)
```

---

### 6. `Services/VectorSearchService.cs`

Queries Azure AI Search using a vector.

Method:
```csharp
Task<List<SearchResult>> SearchAsync(float[] vector, int topK = 5)
```

Where `SearchResult` holds:
- `Id` — document id
- `Content` — text content
- `Source` — table name (Product / Customer / etc.)
- `Score` — relevance score

---

### 7. `Services/PromptBuilder.cs`

Builds the messages array to send to the LLM.

System prompt template:
```
You are an AI assistant for the AdventureWorks business.
Answer only using the context below.
If the context does not contain enough information, say you don't know.
Do not make up data.

Context:
{context_documents}
```

Method:
```csharp
string BuildPrompt(string question, List<SearchResult> context)
```

---

### 8. `Services/ChatService.cs`

Calls `gpt-4o` with the built prompt and returns the answer string.

Method:
```csharp
Task<string> GetAnswerAsync(string systemPrompt, string question)
```

---

### 9. `Controllers/ChatController.cs`

```
POST /api/chat
Body: { "question": "..." }
Returns: { "answer": "...", "sources": [...] }
```

Pipeline:
1. Validate request
2. Call `EmbeddingService.GenerateEmbeddingAsync(question)`
3. Call `VectorSearchService.SearchAsync(vector)`
4. Call `PromptBuilder.BuildPrompt(question, results)`
5. Call `ChatService.GetAnswerAsync(prompt, question)`
6. Return `ChatResponse`

---

### 10. `Program.cs`

- Register all services as singletons
- Add controllers
- Enable CORS (allow React dev origin `http://localhost:5173`)
- Map controller routes

---

## Test Cases — rag-api

### `ChatControllerTests.cs`

| # | Test | Scenario | Expected |
|---|------|----------|----------|
| 1 | `Post_ReturnsAnswer_WhenQuestionIsValid` | Valid question sent | 200 OK with non-empty `answer` |
| 2 | `Post_ReturnsBadRequest_WhenQuestionIsEmpty` | Empty `question` field | 400 Bad Request |
| 3 | `Post_ReturnsBadRequest_WhenQuestionIsNull` | Null body | 400 Bad Request |
| 4 | `Post_ReturnsBadRequest_WhenQuestionExceedsMaxLength` | 1001-char question | 400 Bad Request |
| 5 | `Post_ReturnsSources_WhenDocumentsFound` | Valid question | `sources` array is non-empty |

---

### `VectorSearchServiceTests.cs`

| # | Test | Scenario | Expected |
|---|------|----------|----------|
| 1 | `SearchAsync_ReturnsResults_WhenIndexHasDocuments` | Valid vector | List of SearchResults |
| 2 | `SearchAsync_ReturnsEmpty_WhenNoMatchFound` | Zero-vector | Empty list, no exception |
| 3 | `SearchAsync_RespectsTopK` | TopK = 3 | Returns at most 3 results |
| 4 | `SearchAsync_ThrowsException_WhenInvalidEndpoint` | Bad endpoint config | `InvalidOperationException` |

---

### `PromptBuilderTests.cs`

| # | Test | Scenario | Expected |
|---|------|----------|----------|
| 1 | `BuildPrompt_ContainsQuestion` | Any question | Output contains user question |
| 2 | `BuildPrompt_ContainsContextDocuments` | 3 documents passed | All 3 content strings appear in prompt |
| 3 | `BuildPrompt_HandlesEmptyContext` | No documents found | Prompt still builds without exception |
| 4 | `BuildPrompt_ContainsSystemInstruction` | Any input | "Answer only using the context" present in output |

---

### `ChatServiceTests.cs`

| # | Test | Scenario | Expected |
|---|------|----------|----------|
| 1 | `GetAnswerAsync_ReturnsNonEmptyString` | Valid prompt | Non-empty answer string |
| 2 | `GetAnswerAsync_ThrowsException_OnInvalidApiKey` | Wrong API key | `Azure.RequestFailedException` |
| 3 | `GetAnswerAsync_HandlesMockedResponse` | Mocked OpenAI client | Returns mocked answer |

---

---

# Step 4 — rag-ui (React + Vite)

## Goal

A minimal chat web application that sends questions to `rag-api` and displays AI answers.

---

## Folder Structure

```
rag-ui/
├── package.json
├── vite.config.ts
├── tsconfig.json
├── index.html
├── src/
│   ├── main.tsx
│   ├── App.tsx
│   ├── api/
│   │   └── chatApi.ts            ← POST /api/chat wrapper
│   ├── components/
│   │   ├── ChatWindow.tsx        ← renders message list
│   │   ├── ChatMessage.tsx       ← single message bubble
│   │   ├── ChatInput.tsx         ← input box + send button
│   │   └── SourceBadge.tsx       ← shows source document IDs
│   ├── hooks/
│   │   └── useChat.ts            ← state + API call logic
│   ├── types/
│   │   └── chat.ts               ← ChatMessage, ChatResponse types
│   └── styles/
│       └── App.css
└── tests/
    ├── chatApi.test.ts
    ├── ChatInput.test.tsx
    ├── ChatWindow.test.tsx
    └── useChat.test.ts
```

---

## Files to Create

### 1. `package.json`

- Framework: React 18 + TypeScript
- Build tool: Vite
- Test: Vitest + React Testing Library
- HTTP: native `fetch` (no extra library needed)

---

### 2. `src/types/chat.ts`

```ts
export interface ChatMessage {
  role: "user" | "assistant";
  content: string;
  sources?: string[];
}

export interface ChatResponse {
  answer: string;
  sources: string[];
}
```

---

### 3. `src/api/chatApi.ts`

Wraps `POST /api/chat`.

```ts
async function sendQuestion(question: string): Promise<ChatResponse>
```

- Base URL read from `import.meta.env.VITE_API_URL`
- Throws on non-2xx response

---

### 4. `src/hooks/useChat.ts`

Manages local state:
- `messages: ChatMessage[]`
- `isLoading: boolean`
- `error: string | null`

Exposes:
```ts
sendMessage(question: string): Promise<void>
clearChat(): void
```

---

### 5. `src/components/ChatInput.tsx`

- Text input + Send button
- Disabled when `isLoading === true`
- Clears input after send
- Submits on Enter key

---

### 6. `src/components/ChatMessage.tsx`

- Renders user messages (right-aligned)
- Renders assistant messages (left-aligned)
- Shows `SourceBadge` list under assistant messages

---

### 7. `src/components/SourceBadge.tsx`

- Small pill badge showing a source document ID
- e.g. `product-772`, `orderdetail-1045`

---

### 8. `src/components/ChatWindow.tsx`

- Scrollable message list
- Renders `ChatMessage` for each item in `messages[]`
- Auto-scrolls to bottom on new message

---

### 9. `src/App.tsx`

- Composes `ChatWindow` + `ChatInput`
- Passes `useChat` state and handlers down as props

---

### 10. `vite.config.ts`

```ts
server: {
  proxy: {
    '/api': 'http://localhost:5000'   // proxy to rag-api dev port
  }
}
```

---

## Test Cases — rag-ui

### `chatApi.test.ts`

| # | Test | Scenario | Expected |
|---|------|----------|----------|
| 1 | `sendQuestion_returnsAnswer` | Mock fetch returns 200 with answer | Resolves with `ChatResponse` |
| 2 | `sendQuestion_throwsOnNonOk` | Mock fetch returns 500 | Promise rejects with error |
| 3 | `sendQuestion_throwsOnNetworkError` | fetch throws | Promise rejects |
| 4 | `sendQuestion_sendsCorrectBody` | Valid question | Body JSON contains `question` field |

---

### `useChat.test.ts`

| # | Test | Scenario | Expected |
|---|------|----------|----------|
| 1 | `initialState_isEmpty` | Hook initialized | `messages` is empty, `isLoading` false |
| 2 | `sendMessage_addsUserMessage` | sendMessage called | User message added to `messages` |
| 3 | `sendMessage_addsAssistantMessage` | API resolves | Assistant message added after user message |
| 4 | `sendMessage_setsLoadingTrue_duringRequest` | In-flight request | `isLoading` is true |
| 5 | `sendMessage_setsLoadingFalse_afterResponse` | Request completes | `isLoading` returns to false |
| 6 | `sendMessage_setsError_onFailure` | API rejects | `error` is non-null |
| 7 | `clearChat_resetsMessages` | clearChat called | `messages` array is empty |

---

### `ChatInput.test.tsx`

| # | Test | Scenario | Expected |
|---|------|----------|----------|
| 1 | `renders_inputAndButton` | Component mounts | Input and button visible |
| 2 | `button_disabled_whenLoading` | `isLoading = true` | Send button is disabled |
| 3 | `button_enabled_whenNotLoading` | `isLoading = false` | Send button is enabled |
| 4 | `calls_onSend_onButtonClick` | User types and clicks Send | `onSend` called with input value |
| 5 | `calls_onSend_onEnterKey` | User presses Enter | `onSend` called |
| 6 | `clears_input_afterSend` | After send | Input field is empty |
| 7 | `does_not_call_onSend_whenEmpty` | Empty input + Send | `onSend` NOT called |

---

### `ChatWindow.test.tsx`

| # | Test | Scenario | Expected |
|---|------|----------|----------|
| 1 | `renders_emptyState` | No messages | Empty container renders without error |
| 2 | `renders_userMessage` | 1 user message | User message content visible |
| 3 | `renders_assistantMessage` | 1 assistant message | Assistant message content visible |
| 4 | `renders_sources_underAssistantMessage` | Message has sources | Source badge IDs visible |
| 5 | `renders_multiple_messages_inOrder` | 3 messages | All messages in correct order |

---

---

# Running Everything Together

```bash
# Terminal 1 — run rag-indexer (one-time, already done)
cd rag-indexer && dotnet run

# Terminal 2 — run rag-api
cd rag-api && dotnet run

# Terminal 3 — run rag-ui
cd rag-ui && npm run dev
```

Open: `http://localhost:5173`

---

# Environment Variables Summary

### rag-api `appsettings.json`

| Key | Value |
|-----|-------|
| `AzureOpenAI:Endpoint` | AI Foundry endpoint URL |
| `AzureOpenAI:ApiKey` | AI Foundry API key |
| `AzureOpenAI:EmbeddingDeployment` | `text-embedding-3-small` |
| `AzureOpenAI:ChatDeployment` | `gpt-4o` |
| `AzureSearch:Endpoint` | Azure AI Search endpoint |
| `AzureSearch:ApiKey` | AI Search admin key |
| `AzureSearch:IndexName` | `adventureworks-index` |

### rag-ui `.env`

| Key | Value |
|-----|-------|
| `VITE_API_URL` | `http://localhost:5000` (dev) |
