namespace Identity.Service.Application.Interfaces;

public interface IBrevoEmailService
{
    Task<bool> SendInviteAsync(string toEmail, string inviteLink, string workspaceName, string inviterName);
}
