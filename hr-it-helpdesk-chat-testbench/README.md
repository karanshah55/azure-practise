# HR, IT, and Helpdesk Chat Testbench

A local testbench for manually chatting with an AI support assistant and running repeatable evaluations against synthetic HR, IT, and helpdesk scenarios. It is for testing only: it does not change accounts, create real support tickets, or provision Azure resources.

## Requirements

- .NET 10 SDK
- Node.js is optional and only needed for the browser-script syntax check
- For Foundry mode, an existing Microsoft Foundry project, deployed chat model, and local Azure developer credential

## Run in mock mode

Mock mode is the default and needs no Azure credentials.

```powershell
cd src/HrItHelpdeskChatbot.Web
dotnet restore
dotnet run
```

Open `http://localhost:5080`. Mock replies are deterministic, so the UI and evaluation flow can be tested without calling a model.

## Run against Microsoft Foundry

Configure the existing project endpoint and its model deployment name in the current PowerShell session. Do not put credentials in appsettings or commit them.

```powershell
$env:Chat__Provider = "Foundry"
$env:Chat__FoundryProjectEndpoint = "https://<account>.services.ai.azure.com/api/projects/<project>"
$env:Chat__FoundryDeploymentName = "<existing-deployment-name>"
dotnet run
```

Authenticate locally with a credential supported by `DefaultAzureCredential`, such as Azure CLI, Visual Studio, or Azure Developer CLI. The app does not sign in on your behalf. Foundry chat and evaluation calls can incur model charges.

The project pins `Microsoft.Agents.AI.Foundry` to `1.25.0-preview.261014.1`. Microsoft Agent Framework is in preview; if you add or update the package with `dotnet add package`, include `--prerelease`.

## Test the scenarios

Choose a support domain, send a synthetic chat prompt, or run the six-case evaluation suite. The evaluator checks required and forbidden terms; it is a deterministic smoke/regression check, not an LLM judge or a measure of factual correctness. Results are written to `artifacts/evaluations/` and are ignored by Git.

Use only fictional test inputs. Do not enter real employee, customer, credential, or company-confidential data. Foundry conversations are sent to the configured deployment.

## Configuration

| Setting | Default | Purpose |
| --- | --- | --- |
| `Chat__Provider` | `Mock` | `Mock` or `Foundry` |
| `Chat__FoundryProjectEndpoint` | empty | Existing Foundry project endpoint |
| `Chat__FoundryDeploymentName` | empty | Existing deployed model name |

When `Chat__Provider=Foundry`, both Foundry settings are required. The model deployment is intentionally not hardcoded; use the deployment already available in your project.