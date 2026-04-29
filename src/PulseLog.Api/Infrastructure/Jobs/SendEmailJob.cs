using PulseLog.Api.Features.Common.Abstractions.Services;

namespace PulseLog.Api.Infrastructure.Jobs;

public class SendEmailJob
{
    private readonly IEmailService _emailService;
    private readonly ILogger<SendEmailJob> _logger;

    public SendEmailJob(IEmailService emailService, ILogger<SendEmailJob> logger)
    {
        _emailService = emailService;
        _logger = logger;
    }

    public async Task Execute(int userId, string userEmail, string subject, string body)
    {
        _logger.LogInformation("Hangfire job started: Sending email to user {UserId} at {Email}", userId, userEmail);

        await _emailService.SendEmailToUser(userEmail, subject, body);

        _logger.LogInformation("Hangfire job completed: Email sent to user {UserId}", userId);
    }
}
