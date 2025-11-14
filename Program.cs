using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using OllamaSharp;
using System.Net.Sockets;

Console.WriteLine("Agent Framework + Ollama (phi3:mini) demo");
Console.WriteLine("Ensure Ollama is running: `ollama serve` and model pulled: `ollama pull phi3:mini`.");
Console.WriteLine("Ctrl+C to exit.\n");

var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };

var ollamaUri = new Uri("http://localhost:11434/");

var baseClient = new OllamaApiClient(ollamaUri, "phi3:mini");

IChatClient chatClient = ((IChatClient)baseClient)
    .AsBuilder()
    .Build();

var agent = new ChatClientAgent(
    chatClient,
    instructions: "You are a concise assistant. Keep answers short unless asked to elaborate.");

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
