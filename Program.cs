using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using OllamaSharp;
using Microsoft.Extensions.Configuration;

GetConfig(out string ollamaUriStr, out string model, out string instructions);

Console.WriteLine($"Agent Framework + Ollama ({model}) demo");
Console.WriteLine($"Ensure Ollama is running: `ollama server` and model pulled: ollama:{model}.");
Console.WriteLine($"Ctrl+C to exit{Environment.NewLine}");

var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };

var ollamaUri = new Uri(ollamaUriStr);
var baseClient = new OllamaApiClient(ollamaUri, model);

IChatClient chatClient = ((IChatClient)baseClient)
    .AsBuilder()
    .Build();

var agent = new ChatClientAgent(
    chatClient,
    instructions: instructions);

List<ChatMessage> history = new();

while (!cts.IsCancellationRequested)
{
    Console.Write("You: ");
    var input = Console.ReadLine();
    if (string.IsNullOrWhiteSpace(input)) continue;

    // Add user message to history
    history.Add(new ChatMessage(ChatRole.User, input));

    Console.Write("Agent: ");

    try
    {
        var updates = new List<AgentRunResponseUpdate>();

        await foreach (var update in agent.RunStreamingAsync(history, thread: null, options: null, cts.Token))
        {
            updates.Add(update);
            if (!string.IsNullOrEmpty(update.Text))
            {
                Console.Write(update.Text);
            }
        }

        Console.WriteLine();

        AgentRunResponse response = updates.ToAgentRunResponse();

        history.AddRange(response.Messages.Where(m => m.Role == ChatRole.Assistant));
    }
    catch (OperationCanceledException)
    {
        break;
    }
    catch (Exception ex)
    {
        Console.WriteLine($"{Environment.NewLine}[Error] {ex.Message}{Environment.NewLine}");
    }
}

Console.WriteLine($"{Environment.NewLine}Done.");

static void GetConfig(out string ollamaUriStr, out string model, out string instructions)
{
    var config = new ConfigurationBuilder()
        .SetBasePath(AppContext.BaseDirectory)
        .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
        .AddEnvironmentVariables(prefix: "APP__")
        .Build();

    ollamaUriStr = config["Ollama:Uri"]??string.Empty;
    model = config["Ollama:Model"] ?? string.Empty;
    instructions = config["Agent:Instructions"] ?? string.Empty;
}