using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Identity.Service.Application.Services;

// Brevo Transactional API - sends invite emails (300/day free, same key local/prod)
// Docs: https://developers.brevo.com/docs/transactional-emails
public class BrevoEmailService
{
    private readonly HttpClient _http;
    private readonly IConfiguration _config;
    private readonly ILogger<BrevoEmailService> _logger;

    public BrevoEmailService(HttpClient http, IConfiguration config, ILogger<BrevoEmailService> logger)
    {
        _http = http;
        _config = config;
        _logger = logger;
    }

    public async Task<bool> SendInviteAsync(string toEmail, string inviteLink, string workspaceName, string inviterName)
    {
        var apiKey = _config["Brevo:ApiKey"];
        if (string.IsNullOrEmpty(apiKey) || apiKey.Contains("PASTE_YOUR"))
        {
            _logger.LogWarning("Brevo ApiKey not configured - skipping email to {Email} (invite link: {Link})", toEmail, inviteLink);
            return false; // No key - log and skip, don't fail invite
        }

        var payload = new
        {
            sender = new { name = "FlowBoard", email = "noreply@flowboard.local" },
            to = new[] { new { email = toEmail } },
            subject = $"You've been invited to {workspaceName} on FlowBoard",
            htmlContent = $@"
                <h3>You're invited to join {workspaceName}</h3>
                <p>{inviterName} invited you to collaborate on FlowBoard.</p>
                <p><a href='{inviteLink}' style='background:#4472C4;color:white;padding:10px 20px;text-decoration:none;border-radius:4px;'>Accept Invite</a></p>
                <p>Or copy link: {inviteLink}</p>
                <p>Link expires in 24h.</p>
                <hr/><small>FlowBoard - Enterprise Project Management SaaS</small>"
        };

        var json = JsonSerializer.Serialize(payload);
        var request = new HttpRequestMessage(HttpMethod.Post, "https://api.brevo.com/v3/smtp/email")
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
        request.Headers.Add("api-key", apiKey);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        try
        {
            var response = await _http.SendAsync(request);
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Brevo invite sent to {Email} for workspace {Workspace}", toEmail, workspaceName);
                return true;
            }
            var body = await response.Content.ReadAsStringAsync();
            _logger.LogWarning("Brevo invite failed for {Email}: {Status} {Body}", toEmail, response.StatusCode, body);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Brevo invite exception for {Email}", toEmail);
            return false;
        }
    }
}
