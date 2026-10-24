using System.Diagnostics;
using System.Text.Json;
using HrItHelpdeskChatbot.Web.Models;

namespace HrItHelpdeskChatbot.Web.Services;

public sealed class EvaluationRunner
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    private readonly SupportChatService _chatService;
    private readonly IWebHostEnvironment _environment;
    private readonly string _scenarioFile;

    public EvaluationRunner(
        SupportChatService chatService,
        IWebHostEnvironment environment,
        IConfiguration configuration)
    {
        _chatService = chatService;
        _environment = environment;
        _scenarioFile = Path.Combine(
            environment.ContentRootPath,
            configuration["Evaluation:ScenarioFile"] ?? Path.Combine("Evaluation", "scenarios.json"));
    }

    public IReadOnlyList<ScenarioPreview> GetScenarioPreviews() =>
        LoadScenarios()
            .Select(scenario => new ScenarioPreview(scenario.Id, scenario.Domain, scenario.Title, scenario.Prompt))
            .ToArray();

    public async Task<EvaluationRunResult> RunAsync(CancellationToken cancellationToken)
    {
        var startedAt = DateTimeOffset.UtcNow;
        var runId = Guid.NewGuid().ToString("N");
        var results = new List<EvaluationCaseResult>();

        foreach (var scenario in LoadScenarios())
        {
            cancellationToken.ThrowIfCancellationRequested();
            var stopwatch = Stopwatch.StartNew();
            var response = await _chatService.SendAsync(
                new ChatRequest(scenario.Domain, scenario.Prompt, null),
                cancellationToken);
            stopwatch.Stop();

            var missingTerms = scenario.RequiredTerms
                .Where(term => !response.Reply.Contains(term, StringComparison.OrdinalIgnoreCase))
                .ToArray();
            var foundForbiddenTerms = scenario.ForbiddenTerms
                .Where(term => response.Reply.Contains(term, StringComparison.OrdinalIgnoreCase))
                .ToArray();

            results.Add(new EvaluationCaseResult(
                scenario.Id,
                scenario.Domain,
                scenario.Title,
                missingTerms.Length == 0 && foundForbiddenTerms.Length == 0,
                stopwatch.ElapsedMilliseconds,
                missingTerms,
                foundForbiddenTerms,
                response.Reply));
        }

        var directory = Path.Combine(_environment.ContentRootPath, "artifacts", "evaluations");
        Directory.CreateDirectory(directory);
        var fileName = $"run-{startedAt:yyyyMMdd-HHmmss}-{runId[..8]}.json";
        var resultsPath = Path.Combine(directory, fileName);
        var passed = results.Count(result => result.Passed);
        var run = new EvaluationRunResult(
            runId,
            startedAt,
            _chatService.Provider,
            results.Count,
            passed,
            results.Count - passed,
            Path.Combine("artifacts", "evaluations", fileName).Replace('\\', '/'),
            results);

        await File.WriteAllTextAsync(resultsPath, JsonSerializer.Serialize(run, JsonOptions), cancellationToken);
        return run;
    }

    private IReadOnlyList<EvaluationScenario> LoadScenarios()
    {
        if (!File.Exists(_scenarioFile))
        {
            throw new InvalidOperationException($"Evaluation scenario file was not found: {_scenarioFile}");
        }

        var json = File.ReadAllText(_scenarioFile);
        var scenarios = JsonSerializer.Deserialize<List<EvaluationScenario>>(json, JsonOptions);
        if (scenarios is null || scenarios.Count == 0)
        {
            throw new InvalidOperationException("The evaluation scenario file is empty or invalid.");
        }

        return scenarios;
    }
}



