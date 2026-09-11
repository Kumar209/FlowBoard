using Project.Service.Application.AI.DTOs;

namespace Project.Service.Application.AI.Interfaces;

/// <summary>
/// IAiProvider - abstraction for LLM providers (Gemini 2.5 Flash fixed + Groq llama-3.1-8b selectable). Infrastructure implements via HttpClient (same keys local/prod). DIP - Application depends on interface, mockable without network.
/// </summary>
public interface IAiProvider
{
    string ProviderName { get; } // gemini | groq
    string ModelName { get; } // gemini-2.5-flash | llama-3.1-8b-instant
    Task<AiGenerateResult> GenerateAsync(string prompt, string operation, CancellationToken ct = default);
}
