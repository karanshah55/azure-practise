namespace HrItHelpdeskChatbot.Web.Models;

public sealed record ChatRequest(string Domain, string Message, string? ConversationId);

public sealed record ChatResponse(string ConversationId, string Domain, string Reply, string Provider);

public sealed record RuntimeConfiguration(string Provider, string? DeploymentName, bool FoundryConfigured);

public sealed record EvaluationScenario(
    string Id,
    string Domain,
    string Title,
    string Prompt,
    string[] RequiredTerms,
    string[] ForbiddenTerms);

public sealed record ScenarioPreview(string Id, string Domain, string Title, string Prompt);

public sealed record EvaluationCaseResult(
    string ScenarioId,
    string Domain,
    string Title,
    bool Passed,
    long DurationMilliseconds,
    string[] MissingTerms,
    string[] FoundForbiddenTerms,
    string Reply);

public sealed record EvaluationRunResult(
    string RunId,
    DateTimeOffset StartedAt,
    string Provider,
    int Total,
    int Passed,
    int Failed,
    string ResultsFile,
    IReadOnlyList<EvaluationCaseResult> Cases);

public enum SupportDomain
{
    Hr,
    It,
    Helpdesk
}

public static class SupportDomainParser
{
    public static bool TryParse(string? value, out SupportDomain domain)
    {
        domain = value?.Trim().ToLowerInvariant() switch
        {
            "hr" => SupportDomain.Hr,
            "it" => SupportDomain.It,
            "helpdesk" => SupportDomain.Helpdesk,
            _ => (SupportDomain)(-1)
        };

        return Enum.IsDefined(domain);
    }

    public static string ToKey(this SupportDomain domain) => domain switch
    {
        SupportDomain.Hr => "hr",
        SupportDomain.It => "it",
        SupportDomain.Helpdesk => "helpdesk",
        _ => throw new ArgumentOutOfRangeException(nameof(domain))
    };
}