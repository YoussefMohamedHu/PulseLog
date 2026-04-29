namespace PulseLog.Api.Features.Common.Abstractions.Services;

public interface IEmailService
{
    Task SendEmailToUser(string userEmail, string subject, string body);
    Task BroadcastEmailToUsers(IEnumerable<string> userEmails, string subject, string body);
}
