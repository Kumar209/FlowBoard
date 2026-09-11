using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Project.Service.Application.AI.DTOs;
using Project.Service.Application.AI.Interfaces;

namespace Project.Service.Infrastructure.AI.Providers;

/// <summary>
/// GeminiProvider - calls Gemini 2.5 Flash (gemini-2.5-flash) via Google AI Studio API (15 RPM, 1M TPM, 1500 RPD, same key local/prod).
/// HttpClient with 8s timeout, retry 1x on 429 backoff 2s (like SDD 14). Parses usageMetadata for tokens, returns RawJson for 7.2+ preview. FallbackUsed false.
/// </summary>
public class GeminiProvider : IAiProvider
{
    private readonly HttpClient _http;
    private readonly IConfiguration _config;
    private readonly ILogger<GeminiProvider> _logger;

    public string ProviderName => "gemini";
    public string ModelName => "gemini-2.5-flash";

    public GeminiProvider(HttpClient http, IConfiguration config, ILogger<GeminiProvider> logger)
    {
        _http = http;
        _config = config;
        _logger = logger;
    }

    public async Task<AiGenerateResult> GenerateAsync(string prompt, string operation, CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();
        var apiKey = _config["Gemini:ApiKey"] ?? _config["Gemini__ApiKey"] ?? "";
        if (string.IsNullOrWhiteSpace(apiKey) || apiKey.Contains("PASTE_"))
        {
            _logger.LogWarning("[Gemini] No API key - returning mock (dev without key)");
            var mockJson = BuildMockJson(prompt, operation);
            sw.Stop();
            return new AiGenerateResult(ProviderName, ModelName, mockJson, EstimateTokens(prompt), EstimateTokens(mockJson), (int)sw.ElapsedMilliseconds, false, null);
        }

        var systemInstruction = BuildSystemPrompt(operation);
        var fullPrompt = $"{systemInstruction}\n\nUser prompt: {prompt}";

        var url = $"https://generativelanguage.googleapis.com/v1beta/models/{ModelName}:generateContent?key={apiKey}";
        var body = new
        {
            contents = new[] { new { role = "user", parts = new[] { new { text = fullPrompt } } } },
            generationConfig = new { temperature = 0.7, maxOutputTokens = 1024, responseMimeType = "application/json" }
        };
        var json = JsonSerializer.Serialize(body);
        using var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Retry 1x on 429 with 2s backoff
        for (int attempt = 0; attempt < 2; attempt++)
        {
            try
            {
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                cts.CancelAfter(TimeSpan.FromSeconds(8));
                var resp = await _http.PostAsync(url, content, cts.Token);
                var respText = await resp.Content.ReadAsStringAsync(ct);

                if ((int)resp.StatusCode == 429 && attempt == 0)
                {
                    _logger.LogWarning("[Gemini] 429 rate limit, retry after 2s");
                    await Task.Delay(2000, ct);
                    continue;
                }

                resp.EnsureSuccessStatusCode();

                // Parse Gemini response: candidates[0].content.parts[0].text is JSON string
                var doc = JsonDocument.Parse(respText);
                var root = doc.RootElement;
                string rawJson = "{}";
                int inputTokens = EstimateTokens(fullPrompt);
                int outputTokens = 0;

                if (root.TryGetProperty("candidates", out var candidates) && candidates.GetArrayLength() > 0)
                {
                    var first = candidates[0];
                    if (first.TryGetProperty("content", out var cont) && cont.TryGetProperty("parts", out var parts) && parts.GetArrayLength() > 0)
                    {
                        rawJson = parts[0].GetProperty("text").GetString() ?? "{}";
                    }
                }
                if (root.TryGetProperty("usageMetadata", out var usage))
                {
                    if (usage.TryGetProperty("promptTokenCount", out var pt)) inputTokens = pt.GetInt32();
                    if (usage.TryGetProperty("candidatesTokenCount", out var ct2)) outputTokens = ct2.GetInt32();
                    else outputTokens = EstimateTokens(rawJson);
                }
                else outputTokens = EstimateTokens(rawJson);

                // Ensure valid JSON (Gemini may wrap in markdown)
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
                _logger.LogError(ex, "[Gemini] Generate failed attempt {Attempt}", attempt + 1);
                if (attempt == 1)
                {
                    sw.Stop();
                    throw;
                }
                await Task.Delay(2000, ct);
            }
        }
        sw.Stop();
        throw new InvalidOperationException("[Gemini] Exhausted retries");
    }

    private static string BuildSystemPrompt(string operation) => operation switch
    {
        "draft" => "You are FlowBoard AI. Return JSON { title (string, max 100), description (string, max 500, markdown), checklist (string[] 2-5), labels (string[]), priority (0 Low to 3 Urgent), issueType (Task|Bug|Story), storyPoints (1|2|3|5|8) }. Only JSON.",
        "enhance" => "You are FlowBoard AI. Enhance the given title+description for clarity, add acceptance hints. Return JSON { title, description }. Only JSON.",
        "criteria" => "You are FlowBoard AI. Generate 4-6 acceptance criteria as bullet strings. Return JSON { criteria: string[] }. Only JSON.",
        "breakdown" => "You are FlowBoard AI. Break down the issue into 3-6 subtasks. Return JSON { subtasks: string[] }. Only JSON.",
        _ => "You are FlowBoard AI. Return JSON { title, description }. Only JSON."
    };

    private static string BuildMockJson(string prompt, string operation)
    {
        var safe = prompt.Length > 50 ? prompt[..50] : prompt;
        return operation switch
        {
            "draft" => JsonSerializer.Serialize(new { title = $"[Mock] {safe}", description = $"Mock description for: {safe}\n\n- Covers mobile responsiveness\n- Includes checklist", checklist = new[] { "Analyze mobile breakpoint", "Fix responsive layout", "Test on 320/768/1024" }, labels = new[] { "ai", "mock" }, priority = 2, issueType = "Bug", storyPoints = 3 }),
            "enhance" => JsonSerializer.Serialize(new { title = $"[Enhanced] {safe}", description = $"Enhanced: {safe}\n\n**Steps to reproduce:** 1. Open login on mobile 320px\n**Expected:** responsive\n**Actual:** broken\n**Fix:** Tailwind breakpoints + DaisyUI" }),
            "criteria" => JsonSerializer.Serialize(new { criteria = new[] { "Given login page on 320px, when opened, then all inputs visible without horizontal scroll", "Given invalid email, when submit, then error toast", "Given valid creds, when submit, then redirect to dashboard", "Footer stays anchored" } }),
            "breakdown" => JsonSerializer.Serialize(new { subtasks = new[] { "Create responsive login layout", "Add form validation + error states", "Wire auth API + token storage", "Test on Chrome DevTools mobile + e2e", "Update ActivityLog for audit" } }),
            _ => JsonSerializer.Serialize(new { title = $"[Mock] {safe}", description = "Mock response" })
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
