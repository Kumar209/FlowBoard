using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Project.Service.Application.AI.DTOs;
using Project.Service.Application.AI.Interfaces;

namespace Project.Service.Infrastructure.AI.Providers;

/// <summary>
/// GroqProvider - calls Groq llama-3.1-8b-instant via OpenAI-compatible /openai/v1/chat/completions (free tier, selectable via UI radio). Same keys local/prod. Falls back to mock if no key.
/// </summary>
public class GroqProvider : IAiProvider
{
    private readonly HttpClient _http;
    private readonly IConfiguration _config;
    private readonly ILogger<GroqProvider> _logger;

    public string ProviderName => "groq"; // identifier — never env
    public string ModelName => _config["Groq:Model"] ?? _config["Groq__Model"] ?? "llama-3.1-8b-instant"; // env-fallback: ops can override without rebuild

    public GroqProvider(HttpClient http, IConfiguration config, ILogger<GroqProvider> logger)
    {
        _http = http;
        _config = config;
        _logger = logger;
    }

    public async Task<AiGenerateResult> GenerateAsync(string prompt, string operation, CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();
        var apiKey = _config["Groq:ApiKey"] ?? _config["Groq__ApiKey"] ?? _config["Groq:Key"] ?? "";
        if (string.IsNullOrWhiteSpace(apiKey) || apiKey.Contains("PASTE_"))
        {
            _logger.LogWarning("[Groq] No API key - returning mock (dev without key)");
            var mockJson = BuildMockJson(prompt, operation);
            sw.Stop();
            return new AiGenerateResult(ProviderName, ModelName, mockJson, EstimateTokens(prompt), EstimateTokens(mockJson), (int)sw.ElapsedMilliseconds, false, null);
        }

        var system = BuildSystemPrompt(operation);
        var maxTokens = operation == "enhance" ? 2048 : 1024;
        var body = new
        {
            model = ModelName,
            messages = new[]
            {
                new { role = "system", content = system },
                new { role = "user", content = prompt }
            },
            temperature = 0.7,
            max_tokens = maxTokens,
            response_format = new { type = "json_object" }
        };
        var json = JsonSerializer.Serialize(body);
        for (int attempt = 0; attempt < 2; attempt++)
        {
            try
            {
                using var content = new StringContent(json, Encoding.UTF8, "application/json");
                _http.DefaultRequestHeaders.Clear();
                _http.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                cts.CancelAfter(TimeSpan.FromSeconds(8));
                var resp = await _http.PostAsync("https://api.groq.com/openai/v1/chat/completions", content, cts.Token);
                var respText = await resp.Content.ReadAsStringAsync(ct);

                if ((int)resp.StatusCode == 429 && attempt == 0)
                {
                    _logger.LogWarning("[Groq] 429 rate limit, retry after 2s");
                    await Task.Delay(2000, ct);
                    continue;
                }
                resp.EnsureSuccessStatusCode();

                var doc = JsonDocument.Parse(respText);
                var root = doc.RootElement;
                string rawJson = "{}";
                int inputTokens = EstimateTokens(prompt);
                int outputTokens = 0;

                if (root.TryGetProperty("choices", out var choices) && choices.GetArrayLength() > 0)
                {
                    var msg = choices[0].GetProperty("message");
                    rawJson = msg.GetProperty("content").GetString() ?? "{}";
                }
                if (root.TryGetProperty("usage", out var usage))
                {
                    if (usage.TryGetProperty("prompt_tokens", out var pt)) inputTokens = pt.GetInt32();
                    if (usage.TryGetProperty("completion_tokens", out var ct2)) outputTokens = ct2.GetInt32();
                    else outputTokens = EstimateTokens(rawJson);
                }
                else outputTokens = EstimateTokens(rawJson);

                rawJson = ExtractJson(rawJson);
                sw.Stop();
                return new AiGenerateResult(ProviderName, ModelName, rawJson, inputTokens, outputTokens, (int)sw.ElapsedMilliseconds, false, null);
            }
            catch (OperationCanceledException) when (attempt == 0)
            {
                await Task.Delay(2000, ct);
                continue;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Groq] Generate failed attempt {Attempt}", attempt + 1);
                if (attempt == 1) throw;
                await Task.Delay(2000, ct);
            }
        }
        sw.Stop();
        throw new InvalidOperationException("[Groq] Exhausted retries");
    }

    private static string BuildSystemPrompt(string operation) => operation switch
    {
        "draft" => "You are FlowBoard AI. Return JSON { title, description, checklist (array 2-5), labels (array), priority (0-3), issueType (Task|Bug|Story), storyPoints (1|2|3|5|8) }. Only JSON.",
        "enhance" => "You are FlowBoard AI. Enhance title+description. Return JSON { title, description }. Only JSON.",
        "criteria" => "You are FlowBoard AI. Generate 4-6 acceptance criteria. Return JSON { criteria: string[] }. Only JSON.",
        "breakdown" => "You are FlowBoard AI. Break down into 3-6 subtasks. Return JSON { subtasks: string[] }. Only JSON.",
        _ => "You are FlowBoard AI. Return JSON { title, description }. Only JSON."
    };

    private static string BuildMockJson(string prompt, string operation)
    {
        var safe = prompt.Length > 50 ? prompt[..50] : prompt;
        return operation switch
        {
            "draft" => JsonSerializer.Serialize(new { title = $"[Groq Mock] {safe}", description = $"Groq mock description for: {safe}", checklist = new[] { "Step 1 analysis", "Step 2 implementation", "Step 3 verification" }, labels = new[] { "groq", "mock" }, priority = 1, issueType = "Task", storyPoints = 2 }),
            "enhance" => JsonSerializer.Serialize(new { title = $"[Groq Enhanced] {safe}", description = $"Groq enhanced: {safe}" }),
            "criteria" => JsonSerializer.Serialize(new { criteria = new[] { "Criteria 1 groq", "Criteria 2", "Criteria 3", "Criteria 4" } }),
            "breakdown" => JsonSerializer.Serialize(new { subtasks = new[] { "Subtask A groq", "Subtask B", "Subtask C" } }),
            _ => JsonSerializer.Serialize(new { title = $"[Groq Mock] {safe}", description = "Groq mock" })
        };
    }

    private static string ExtractJson(string text)
    {
        var trimmed = text.Trim();
        if (trimmed.StartsWith("```"))
        {
            var start = trimmed.IndexOf('{');
            var end = trimmed.LastIndexOf('}');
            if (start >= 0 && end > start) return trimmed.Substring(start, end - start + 1);
        }
        return trimmed;
    }

    private static int EstimateTokens(string text) => Math.Max(1, text.Length / 4);
}
