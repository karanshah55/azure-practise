# SalesBilling Copilot

## Project summary
A lightweight AI-assisted support chatbot for sales and billing questions. It routes customer requests into sales or billing intents, answers from a curated FAQ knowledge base, and provides a clean local web experience for demo and internal use.

## Folder name
- sales-billing-chatbot

## Stack
- Frontend: React + TypeScript + Vite
- Backend: Python + FastAPI
- Rule engine: Python logic for intent routing and response selection
- Knowledge base: JSON/YAML FAQ data store
- Local run: simple npm + pip commands with a single dev entrypoint

## Core features
- Chat interface with message history and quick reply suggestions
- Rule-based intent detection for sales vs billing requests
- FAQ knowledge base for common product questions and billing policies
- Response templates for pricing, plans, invoices, payments, refunds, and subscriptions
- Graceful fallback for unsupported queries
- Basic analytics logging for intent counts and top issues
- Local-only configuration for easy setup and testing

## Suggested project structure
- frontend/   - React chat UI
- backend/    - FastAPI app and intent router
- data/       - FAQ knowledge base and sample conversations
- scripts/    - local start scripts and setup helpers
- README.md   - run instructions and usage notes

## Execution steps
1. Initialize the frontend app with React + Vite and TypeScript.
2. Create the backend API with FastAPI and a /chat endpoint.
3. Build a FAQ dataset with sales and billing questions, intents, and answers.
4. Implement rule-based routing:
   - sales keywords: pricing, plan, feature, demo, upgrade, contract
   - billing keywords: invoice, refund, payment, tax, subscription, charge
5. Add a response generator that matches the detected intent to the correct FAQ answer.
6. Connect the UI to the backend and display responses in the chat panel.
7. Add a local-run script for frontend + backend startup.
8. Test common scenarios: plan questions, invoice issues, refund requests, unsupported queries.

## Success criteria
- Users can chat naturally in a clean web interface.
- Sales and billing requests are correctly routed.
- FAQ answers are returned quickly and consistently.
- The app runs locally without external dependencies beyond standard dev tooling.
