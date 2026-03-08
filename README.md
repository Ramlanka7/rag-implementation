# AdventureWorks RAG Assistant

A Retrieval Augmented Generation (RAG) application built using **.NET 10, React, Azure OpenAI, Azure AI Search, and Azure SQL**.

This project demonstrates how to build an **enterprise-style AI assistant** that answers questions about the **AdventureWorks database** using vector search and large language models.

---

# Architecture

```
React UI
   │
   ▼
.NET 10 API (RAG Orchestrator)
   │
   ├── Azure OpenAI
   │       ├─ Embeddings
   │       └─ LLM (GPT-4o / GPT-4o-mini)
   │
   ├── Azure AI Search (Vector Database)
   │
   └── Azure SQL (AdventureWorks)

Background Worker (.NET)
   │
   └── Indexing / Ingestion Service
```

---

# Components

## 1. React UI

Provides a simple **chat interface** for users to ask questions about the AdventureWorks dataset.

Responsibilities:

* Send user questions to the API
* Display AI responses
* Maintain chat history
* Stream responses (optional)

Example request:

```
POST /api/chat

{
  "question": "Which products generated the highest sales in 2013?"
}
```

---

# 2. .NET 10 API (RAG Orchestrator)

The API coordinates the entire **RAG pipeline**.

Responsibilities:

1. Receive user question
2. Generate embedding using Azure OpenAI
3. Perform vector search in Azure AI Search
4. Retrieve relevant documents
5. Construct prompt with context
6. Send prompt to LLM
7. Return AI response

### RAG Query Flow

```
User Question
      │
Create Embedding
      │
Vector Search
      │
Retrieve Context
      │
Prompt Construction
      │
LLM Generation
      │
Response to User
```

---

# 3. Azure OpenAI

Used for both **embeddings** and **LLM inference**.

### Embedding Model

```
text-embedding-3-small
```

Used to convert:

* documents
* user questions

into vector embeddings.

### LLM Model

```
gpt-4o
or
gpt-4o-mini
```

Used to generate the final natural language response.

---

# 4. Azure AI Search (Vector Database)

Stores vectorized documents and enables **semantic similarity search**.

Each indexed document contains:

```
{
  id
  content
  vector
  metadata
}
```

Example document:

```
Mountain Bike is a red bicycle sold in the Bikes category priced at $1500.
```

When a user asks a question, the system retrieves the **most relevant documents** using vector similarity.

---

# 5. Azure SQL (AdventureWorks)

The relational database containing structured business data.

Example tables:

* Production.Product
* Sales.Customer
* Sales.SalesOrderHeader
* Sales.SalesOrderDetail

This database acts as the **source of truth**.

---

# 6. Background Worker (.NET)

The background worker is responsible for **data ingestion and indexing**.

It extracts data from SQL, converts it into semantic documents, generates embeddings, and stores them in Azure AI Search.

### Indexing Pipeline

```
Azure SQL
   │
Extract Rows
   │
Transform to Text Documents
   │
Generate Embeddings
   │
Store in Azure AI Search
```

Example generated document:

```
The Mountain Bike is a red bicycle in the Bikes category priced at $1500.
```

This document is converted into a vector and indexed.

The indexing service may run:

* on startup
* on schedule
* via event triggers

---

# End-to-End Query Flow

```
User
 │
 ▼
React UI
 │
 ▼
.NET API
 │
 ├─ Generate embedding
 │
 ├─ Query Azure AI Search
 │
 ├─ Retrieve top documents
 │
 ├─ Construct prompt
 │
 ▼
Azure OpenAI (LLM)
 │
 ▼
AI Generated Answer
 │
 ▼
React UI
```

---

# Project Structure

```
adventureworks-rag
│
├── rag-ui
│     React frontend
│
├── rag-api
│     ASP.NET Core API (.NET 10)
│
├── rag-indexer
│     .NET Worker Service
│
└── infrastructure
      Azure deployment scripts
```

---

# Example Questions

Users can ask questions such as:

```
Which products generated the highest revenue?

Which customers placed the most orders?

Which products sold the most in 2013?

Which customers ordered mountain bikes?
```

The system retrieves relevant context and generates accurate answers using RAG.

---

# Technologies Used

* React
* .NET 10
* Azure OpenAI
* Azure AI Search
* Azure SQL Database
* AdventureWorks Dataset

---

# Future Improvements

Possible enhancements:

* Hybrid search (vector + keyword)
* Reranking models
* Streaming responses
* Semantic caching
* Conversation memory
* Agent-based workflows

---

# Goal

This project demonstrates how to build a **modern enterprise RAG application** using the Microsoft AI ecosystem and the .NET stack.
