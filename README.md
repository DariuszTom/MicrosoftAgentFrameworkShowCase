# Agent Framework + Ollama Demo (.NET 9 / C# 13)

Minimal console sample using `Microsoft.Agents.AI`, `Microsoft.Extensions.AI` and `OllamaSharp` to run a streaming chat agent against the local Ollama `phi3:mini` model.

## Prerequisites
- .NET 9 SDK
- Ollama installed (https://ollama.com)
- Model pulled locally: `ollama pull phi3:mini`

## Streaming Output
Responses stream token-by-token; collected updates are converted to a final `AgentRunResponse` and assistant messages appended to history.

## Troubleshooting
Connection refused: ensure `ollama serve` is running and model is pulled.
Interrupt: Ctrl+C triggers graceful cancellation.

## Notes
- Adjust model by changing `"phi3:mini"` to any locally available Ollama model.
- Extend instructions for different agent behaviors.

## License
MIT
