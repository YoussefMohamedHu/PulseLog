using Microsoft.Extensions.Logging;
using PulseLog.Api.Features.Common.Abstractions.Services;

namespace PulseLog.Api.Infrastructure.Services;

public class EmailService(ILogger<EmailService> logger) : IEmailService
{
    public Task SendEmailToUser(string userEmail, string subject, string body)
    {
        logger.LogInformation("Sending email to {UserEmail} with subject {Subject}", userEmail, subject);
        return Task.CompletedTask;
    }

    public Task BroadcastEmailToUsers(IEnumerable<string> userEmails, string subject, string body)
    {
        var emailList = string.Join(", ", userEmails);
        logger.LogInformation("Broadcasting email to {UserEmails} with subject {Subject}", emailList, subject);
        return Task.CompletedTask;
    }
}
