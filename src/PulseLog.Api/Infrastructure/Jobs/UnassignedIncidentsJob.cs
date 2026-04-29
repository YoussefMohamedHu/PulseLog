using Hangfire;
using Microsoft.EntityFrameworkCore;
using PulseLog.Api.Domain.ValueObjects;
using PulseLog.Api.Infrastructure.Persistence;

namespace PulseLog.Api.Infrastructure.Jobs;

public class UnassignedIncidentsJob(AppDbContext dbContext, ILogger<UnassignedIncidentsJob> logger)
{
    private readonly AppDbContext _dbContext = dbContext;
    private readonly ILogger<UnassignedIncidentsJob> _logger = logger;

    public async Task Execute()
    {
        _logger.LogInformation("JobStarted: {JobName}", nameof(UnassignedIncidentsJob));

        var threshold = DateTime.UtcNow.AddMinutes(-15);

        var unassignedIncidents = await _dbContext.Incidents
        .Where(i => (i.Status == IncidentStatus.Open) &&
         (i.AssignedTo == null) &&
         (i.Priority == IncidentPriority.Critical || i.Priority == IncidentPriority.High) &&
         (i.CreatedAt <threshold))
         .ToListAsync();

         if(unassignedIncidents.Count == 0)
        {
            _logger.LogInformation("No unassigned incidents found matching criteria");
            return;
        }

        _logger.LogInformation("UnassignedIncidentsFound: {Count}", unassignedIncidents.Count);

        var recipients = await _dbContext.Users
        .Where(u => u.Role == UserRole.Admin || u.Role == UserRole.Agent)
        .ToListAsync();

        foreach(var incident in unassignedIncidents)
        {
            foreach(var recipient in recipients)
            {
                BackgroundJob.Enqueue<SendEmailJob>(job => job.Execute(
                    recipient.Id,
                    recipient.Email,
                    "Action Required: Unassigned High Priority Incident",
                    $"Incident #{incident.Id} ({incident.Title}) has been unassigned for more than 15 minutes."

                ));

                _logger.LogInformation("NotificationDispatched: {IncidentId} to User {UserId}", incident.Id, recipient.Id);
            }
        }

        _logger.LogInformation("JobCompleted: {JobName}. Total Incidents: {Count}", nameof(UnassignedIncidentsJob),unassignedIncidents.Count);
    }    

}