using PulseLog.Api.Features.Common.Abstractions.Services;

namespace PulseLog.Api.Infrastructure.Jobs;

public class BroadcastEmailJob
{
    private readonly IEmailService _emailService;
    private readonly ILogger<BroadcastEmailJob> _logger;

    public BroadcastEmailJob(IEmailService emailService, ILogger<BroadcastEmailJob> logger)
    {
        _emailService = emailService;
        _logger = logger;
    }

    public async Task Execute(IEnumerable<int> userIds, IEnumerable<string> userEmails, string subject, string body)
    {
        var userIdList = userIds.ToList();
        var emailList = userEmails.ToList();

        _logger.LogInformation("Hangfire job started: Broadcasting email to {Count} users", userIdList.Count);

        await _emailService.BroadcastEmailToUsers(emailList, subject, body);

        _logger.LogInformation("Hangfire job completed: Email broadcasted to {Count} users", userIdList.Count);
    }
}
