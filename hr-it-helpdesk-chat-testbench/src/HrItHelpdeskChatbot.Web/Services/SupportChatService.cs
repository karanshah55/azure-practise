using System.Collections.Concurrent;
using Azure.AI.Projects;
using Azure.Identity;
using HrItHelpdeskChatbot.Web.Models;
using Microsoft.Agents.AI;

namespace HrItHelpdeskChatbot.Web.Services;

public sealed class SupportChatService
{
    private readonly ConcurrentDictionary<SupportDomain, AIAgent> _agents = new();
    private readonly ConcurrentDictionary<string, ConversationState> _conversations = new();
    private readonly DefaultAzureCredential _credential = new();
    private readonly string _foundryEndpoint;
    private readonly string _deploymentName;

    public SupportChatService(IConfiguration configuration)
    {
        Provider = NormalizeProvider(configuration["Chat:Provider"]);
        _foundryEndpoint = configuration["Chat:FoundryProjectEndpoint"] ?? string.Empty;
        _deploymentName = configuration["Chat:FoundryDeploymentName"] ?? string.Empty;
    }

    public string Provider { get; }

    public string DeploymentName => _deploymentName;

    public bool FoundryConfigured =>
        Uri.TryCreate(_foundryEndpoint, UriKind.Absolute, out var endpoint) &&
        endpoint.Scheme == Uri.UriSchemeHttps &&
        !string.IsNullOrWhiteSpace(_deploymentName);

    public async Task<ChatResponse> SendAsync(ChatRequest request, CancellationToken cancellationToken)
    {
        if (!SupportDomainParser.TryParse(request.Domain, out var domain))
        {
            throw new ArgumentException("Choose HR, IT, or helpdesk.");
        }

        var conversationId = request.ConversationId;
        if (string.IsNullOrWhiteSpace(conversationId))
        {
            conversationId = Guid.NewGuid().ToString("N");
        }
        else if (!Guid.TryParseExact(conversationId, "N", out _))
        {
            throw new ArgumentException("The conversation identifier is invalid.");
        }

        var reply = Provider == "Mock"
            ? GetMockReply(domain)
            : await GetFoundryReplyAsync(domain, request.Message, conversationId, cancellationToken);

        return new ChatResponse(conversationId, domain.ToKey(), reply, Provider);
    }

    private async Task<string> GetFoundryReplyAsync(
        SupportDomain domain,
        string message,
        string conversationId,
        CancellationToken cancellationToken)
    {
        if (!FoundryConfigured)
        {
            throw new InvalidOperationException(
                "Foundry mode requires Chat__FoundryProjectEndpoint and Chat__FoundryDeploymentName.");
        }

        var agent = _agents.GetOrAdd(domain, CreateAgent);
        var conversation = await GetConversationAsync(conversationId, domain, agent);
        await conversation.Gate.WaitAsync(cancellationToken);

        try
        {
            var response = await agent.RunAsync(message, conversation.Session, cancellationToken: cancellationToken);
            return response.Text;
        }
        finally
        {
            conversation.Gate.Release();
        }
    }

    private AIAgent CreateAgent(SupportDomain domain)
    {
        var projectClient = new AIProjectClient(new Uri(_foundryEndpoint), _credential);
        return projectClient.AsAIAgent(
            model: _deploymentName,
            name: $"support-{domain.ToKey()}",
            instructions: GetInstructions(domain));
    }

    private async Task<ConversationState> GetConversationAsync(
        string conversationId,
        SupportDomain domain,
        AIAgent agent)
    {
        if (_conversations.TryGetValue(conversationId, out var existing))
        {
            if (existing.Domain != domain)
            {
                throw new ArgumentException("Start a new conversation when switching support domains.");
            }

            return existing;
        }

        var created = new ConversationState(domain, await agent.CreateSessionAsync());
        var conversation = _conversations.GetOrAdd(conversationId, created);
        if (conversation.Domain != domain)
        {
            throw new ArgumentException("Start a new conversation when switching support domains.");
        }

        return conversation;
    }

    private static string NormalizeProvider(string? provider)
    {
        if (string.IsNullOrWhiteSpace(provider) || provider.Equals("Mock", StringComparison.OrdinalIgnoreCase))
        {
            return "Mock";
        }

        if (provider.Equals("Foundry", StringComparison.OrdinalIgnoreCase))
        {
            return "Foundry";
        }

        throw new InvalidOperationException("Chat:Provider must be Mock or Foundry.");
    }

    private static string GetInstructions(SupportDomain domain)
    {
        var scope = domain switch
        {
            SupportDomain.Hr => "You are an HR information assistant for fictional test scenarios. Explain sample leave, benefits, and policy processes. Do not make employment decisions or claim that a request was approved.",
            SupportDomain.It => "You are an IT help assistant for fictional test scenarios. Offer safe troubleshooting steps. Never ask for passwords or verification codes, and never claim that an account or device was changed.",
            SupportDomain.Helpdesk => "You are a helpdesk assistant for fictional test scenarios. Help clarify issues and explain escalation steps. Do not claim that a real support ticket was created or updated.",
            _ => throw new ArgumentOutOfRangeException(nameof(domain))
        };

        return $"{scope} Use only synthetic information. Do not request, repeat, or expose personal data, credentials, or confidential company information. When a policy or fact is not provided, say so and recommend contacting the appropriate human team. You have no external action tools.";
    }

    private static string GetMockReply(SupportDomain domain) => domain switch
    {
        SupportDomain.Hr => "For this synthetic HR scenario, check the sample leave policy. If it does not cover the situation, contact HR. I did not access or change any employee records.",
        SupportDomain.It => "Use the test password-reset or MFA recovery guide. Never share passwords or verification codes. No real account or device was changed.",
        SupportDomain.Helpdesk => "This is a simulated support ticket. Gather the test error details and escalate to the helpdesk queue if unresolved. No real ticket was created.",
        _ => throw new ArgumentOutOfRangeException(nameof(domain))
    };

    private sealed class ConversationState(SupportDomain domain, AgentSession session)
    {
        public SupportDomain Domain { get; } = domain;
        public AgentSession Session { get; } = session;
        public SemaphoreSlim Gate { get; } = new(1, 1);
    }
}