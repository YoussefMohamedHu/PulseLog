using Microsoft.Extensions.Logging;
using Polly;
using Polly.Registry;
using PulseLog.Api.Features.Common.Abstractions.Services;

namespace PulseLog.Api.Infrastructure.Services;

public class EmailService(ILogger<EmailService> logger, ResiliencePipelineProvider<string> pipelineProvider) : IEmailService
{
    public async Task SendEmailToUser(string userEmail, string subject, string body)
    {
        var pipeline = pipelineProvider.GetPipeline("email-pipeline");

        await pipeline.ExecuteAsync(async token =>
        {
            logger.LogInformation("Sending email to {UserEmail} with subject {Subject}", userEmail, subject);
            // In a real scenario, this is where the SMTP or API call happens
            await Task.CompletedTask;
        });
    }

    public async Task BroadcastEmailToUsers(IEnumerable<string> userEmails, string subject, string body)
    {
        var pipeline = pipelineProvider.GetPipeline("email-pipeline");

        await pipeline.ExecuteAsync(async token =>
        {
            var emailList = string.Join(", ", userEmails);
            logger.LogInformation("Broadcasting email to {UserEmails} with subject {Subject}", emailList, subject);
            await Task.CompletedTask;
        });
    }
}
