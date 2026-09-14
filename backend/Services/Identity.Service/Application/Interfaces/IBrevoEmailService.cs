namespace Identity.Service.Application.Interfaces;

public interface IBrevoEmailService
{
    Task<bool> SendInviteAsync(string toEmail, string inviteLink, string workspaceName, string inviterName);
    Task<bool> SendEmailAsync(string toEmail, string subject, string htmlContent);
}
