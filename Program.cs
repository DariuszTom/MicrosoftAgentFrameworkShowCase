using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using OllamaSharp;
using System.Net.Sockets;
using Microsoft.Extensions.Configuration;

Console.WriteLine("Agent Framework + Ollama (phi3:mini) demo");
Console.WriteLine("Ensure Ollama is running: `ollama serve` and model pulled: `ollama pull phi3:mini`.");
Console.WriteLine("Ctrl+C to exit.\n");

var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };


GetConfig(out string ollamaUriStr, out string model, out string instructions);

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
        if (response.Messages != null)
        {
            foreach (var m in response.Messages)
            {
                if (m.Role == ChatRole.Assistant)
                {
                    history.Add(m);
                }
            }
        }
    }
    catch (OperationCanceledException)
    {
        break;
    }
    catch (HttpRequestException httpEx) when (httpEx.InnerException is SocketException se && se.SocketErrorCode == SocketError.ConnectionRefused)
    {
        Console.WriteLine($"\n[Connection Error] {httpEx.Message}\n" +
                          "Ollama became unreachable. Verify `ollama serve` is still running.\n");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"\n[Error] {ex.Message}\n");
    }
}

Console.WriteLine("\nDone.");

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