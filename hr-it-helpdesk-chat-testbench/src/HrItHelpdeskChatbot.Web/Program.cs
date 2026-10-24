using HrItHelpdeskChatbot.Web.Models;
using HrItHelpdeskChatbot.Web.Services;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<SupportChatService>();
builder.Services.AddSingleton<EvaluationRunner>();

var app = builder.Build();
app.UseDefaultFiles();
app.UseStaticFiles();

app.MapGet("/api/config", (SupportChatService chatService) =>
    Results.Ok(new RuntimeConfiguration(
        chatService.Provider,
        chatService.Provider == "Foundry" ? chatService.DeploymentName : null,
        chatService.FoundryConfigured)));

app.MapGet("/api/scenarios", (EvaluationRunner runner) =>
    Results.Ok(runner.GetScenarioPreviews()));

app.MapPost("/api/chat", async (
    ChatRequest request,
    SupportChatService chatService,
    ILoggerFactory loggerFactory,
    CancellationToken cancellationToken) =>
{
    if (string.IsNullOrWhiteSpace(request.Message) || request.Message.Length > 2000)
    {
        return Results.BadRequest(new { error = "Enter a message between 1 and 2000 characters." });
    }

    try
    {
        return Results.Ok(await chatService.SendAsync(request, cancellationToken));
    }
    catch (ArgumentException exception)
    {
        return Results.BadRequest(new { error = exception.Message });
    }
    catch (InvalidOperationException exception)
    {
        return Results.BadRequest(new { error = exception.Message });
    }
    catch (Exception exception)
    {
        loggerFactory.CreateLogger("ChatEndpoint").LogError(exception, "Chat request failed.");
        return Results.Problem("The selected provider could not complete the request. Check its local configuration and authentication.");
    }
});

app.MapPost("/api/evaluations/run", async (
    EvaluationRunner runner,
    ILoggerFactory loggerFactory,
    CancellationToken cancellationToken) =>
{
    try
    {
        return Results.Ok(await runner.RunAsync(cancellationToken));
    }
    catch (InvalidOperationException exception)
    {
        return Results.BadRequest(new { error = exception.Message });
    }
    catch (Exception exception)
    {
        loggerFactory.CreateLogger("EvaluationEndpoint").LogError(exception, "Evaluation run failed.");
        return Results.Problem("The evaluation run failed. Check the selected provider configuration and local logs.");
    }
});

app.Run();