# Financial Agent

An AI-powered financial research agent built on the Microsoft Azure stack, capable of autonomously retrieving live market data, analyzing fundamentals, and synthesizing investment research — all driven by natural language.

Built with **C# / .NET 8**, **Microsoft Semantic Kernel**, **Azure OpenAI**, and the **Alpha Vantage API**.

---

## What It Does

Ask the agent a natural language question. It decides which tools to call, retrieves live data, chains the results together, and returns a grounded, cited answer.

```
You: Should I be bullish on NVDA right now?

Agent: NVIDIA (NVDA) is currently trading at $192.85 (+0.68% today).
       Fundamentals show a market cap of $4,663.7B with a P/E of 47.41 and
       analyst target of $253.99 — suggesting ~31% upside. Recent earnings
       have consistently beaten estimates. News sentiment is bullish driven
       by AI infrastructure demand and analyst upgrades. Overall: cautiously
       bullish with elevated valuation risk.
```

---

## Architecture

```
┌─────────────────────┐
│   User / REST API   │
└────────┬────────────┘
         │ Natural language question
         ▼
┌─────────────────────────────────────────────┐
│           Semantic Kernel Agent              │
│                                              │
│  ┌──────────────────────────────────────┐   │
│  │         Azure OpenAI GPT-4o-mini     │   │
│  │   Reasons over which tools to call   │   │
│  └──────────────┬───────────────────────┘   │
│                 │ Function calling           │
│  ┌──────────────▼───────────────────────┐   │
│  │          FinancialTools Plugin        │   │
│  │  • GetStockPrice                     │   │
│  │  • GetFundamentals                   │   │
│  │  • GetNewsSentiment                  │   │
│  │  • GetEarnings                       │   │
│  │  • CompareStocks                     │   │
│  │  • CalculateReturn                   │   │
│  └──────────────┬───────────────────────┘   │
└─────────────────┼───────────────────────────┘
                  │ Live API calls
                  ▼
       ┌──────────────────────┐
       │   Alpha Vantage API  │
       │  Real-time market    │
       │  data & news         │
       └──────────────────────┘
```

---

## Features

- **Autonomous Tool Selection** — The agent decides which tools to call based on the question. No hardcoded routing.
- **Live Market Data** — Real-time stock prices and intraday change via Alpha Vantage
- **Fundamental Analysis** — P/E ratio, EPS, market cap, 52-week range, analyst target prices
- **Earnings Intelligence** — Last 4 quarters of EPS actuals vs estimates with beat/miss percentages
- **News Sentiment** — Live headline sentiment scoring per ticker from Alpha Vantage
- **Stock Comparison** — Side-by-side analysis of two tickers across price and sentiment
- **Return Calculator** — Percentage return between any two price points
- **REST API** — ASP.NET Core Web API with Swagger UI for interactive demos

---

## Tech Stack

| Component | Technology |
|---|---|
| Language | C# / .NET 8 |
| Agent Orchestration | Microsoft Semantic Kernel |
| LLM | Azure OpenAI (GPT-4o-mini) |
| Market Data | Alpha Vantage API |
| API Framework | ASP.NET Core Web API |
| Configuration | .NET User Secrets |

---

## Project Structure

```
FinancialAgent.sln
├── FinancialAgent.Core          # Agent tools and shared logic
│   └── FinancialTools.cs            # Semantic Kernel plugin with all tools
├── FinancialAgent.Runner        # Console app for interactive Q&A
└── FinancialAgent.Api           # ASP.NET Core REST API
```

---

## Getting Started

### Prerequisites

- .NET 8 SDK
- Azure subscription with Azure OpenAI resource
- `gpt-4o-mini` deployment
- Alpha Vantage API key (free at [alphavantage.co](https://www.alphavantage.co))

### Configuration

This project uses .NET User Secrets. In both `FinancialAgent.Runner` and `FinancialAgent.Api`:

```bash
dotnet user-secrets set "AzureOpenAI:Endpoint" "https://YOUR_RESOURCE.openai.azure.com/"
dotnet user-secrets set "AzureOpenAI:Key" "YOUR_KEY"
dotnet user-secrets set "AzureOpenAI:ChatDeployment" "gpt-4o-mini"
dotnet user-secrets set "AlphaVantage:Key" "YOUR_KEY"
```

### Run the Console Agent

Set `FinancialAgent.Runner` as startup and run. The agent enters an interactive loop:

```
Financial Agent ready. Type 'quit' to exit.

You: Compare NVDA and MSFT and tell me which looks more attractive
Agent: ...
```

### Run the API

Set `FinancialAgent.Api` as startup and run. Navigate to:

```
https://localhost:{port}/swagger
```

### Example Request

```http
POST /api/agent/ask
Content-Type: application/json

{
  "question": "Has Apple been beating earnings estimates and what is the current sentiment?"
}
```

### Example Response

```json
{
  "answer": "Apple Inc. (AAPL) has consistently beaten earnings estimates over the last four quarters, with surprises ranging from 1.85% to 9.79%. Current news sentiment is neutral to slightly bullish, with headlines noting stable iPhone sales and services growth offsetting hardware concerns."
}
```

---

## How the Agent Works

Unlike a traditional API that maps inputs to fixed outputs, this agent uses **Semantic Kernel's function calling** to reason dynamically:

1. The user's question is sent to GPT-4o-mini along with descriptions of all available tools
2. The model decides which tools are needed and in what order
3. Semantic Kernel executes the selected tools and returns results to the model
4. The model synthesizes the tool outputs into a coherent, grounded response

This means the agent handles novel questions gracefully — if asked to compare three stocks, it calls the comparison tools multiple times without any additional code.

---

## Related Projects

- [financial-rag-demo](https://github.com/mjdevaccount/financial-rag-demo) — Financial document Q&A using RAG, Azure OpenAI, and Azure AI Search

---

## License

MIT
