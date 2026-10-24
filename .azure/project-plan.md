# HR, IT, and Helpdesk Chat Testbench

## Goal

Create a local browser-based testbench for exercising an AI support assistant across synthetic HR, IT, and helpdesk scenarios. This is a testing tool, not a production support system. It does not modify employee accounts, create real tickets, or provision Azure resources.

## Stack and Architecture

- .NET 10 and ASP.NET Core minimal APIs serve a local browser UI.
- Microsoft Agent Framework connects the app-owned agent to an existing Microsoft Foundry project and model deployment through `AIProjectClient`.
- A deterministic mock provider is the default so the UI and evaluator can run without cloud credentials.
- Each browser conversation has an in-memory Agent Framework session. Sessions are not persisted.
- Synthetic scenario JSON covers all three support domains. The local evaluator checks required and forbidden terms and records results under an ignored artifacts directory.

## Safety and Configuration

- HR, IT, and helpdesk instructions prohibit requesting or exposing credentials and personal data.
- Scenario content is fictional. Do not enter real employee, customer, or company data.
- Foundry endpoint, deployment name, and authentication are configured outside source control.
- Evaluation outputs are local and ignored by Git. The app performs no external actions.

## Verification

- Verify scenario data parses and browser JavaScript passes `node --check`.
- Build and run in mock mode when a .NET 10 SDK is available.
- Run the repeatable evaluation suite in mock mode and verify all synthetic cases pass.
- Exercise Foundry mode only after a user supplies an existing project endpoint and deployed model name, then authenticates locally.

## Current Environment Constraint

The .NET SDK is not installed. The user approved creating source files without installing it, so build and runtime checks are deferred until the SDK is available.