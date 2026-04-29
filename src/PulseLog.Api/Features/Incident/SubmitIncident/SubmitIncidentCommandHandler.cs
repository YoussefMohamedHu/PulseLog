using PulseLog.Api.Features.Common.Abstractions;
using PulseLog.Api.Infrastructure.Persistence;
using PulseLog.Api.Domain.Entities;
using MediatR;
using PulseLog.Api.Domain.ValueObjects;
using PulseLog.Api.Features.Common.Exceptions;
using PulseLog.Api.Infrastructure.Jobs;
using Hangfire;
namespace PulseLog.Api.Features.Incident.SubmitIncident;

public class SubmitIncidentCommandHandler : IRequestHandler<SubmitIncidentCommand, IncidentResponse>
{
    private readonly AppDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<SubmitIncidentCommandHandler> _logger;

    public SubmitIncidentCommandHandler(ICurrentUser currentUser,AppDbContext dbContext, ILogger<SubmitIncidentCommandHandler> logger)
    {
        _currentUser = currentUser;
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<IncidentResponse> Handle(SubmitIncidentCommand command, CancellationToken ct)
    {
        
        
        var userId = _currentUser.GetCurrentUserId();
        var userRole = _currentUser.GetCurrentUserRole();

        if(userRole != UserRole.Reporter)
        {
            _logger.LogWarning("User with id {UserId} tried to submit an incident but is not a reporter", userId);

            throw new UnauthorizedAccessException("User is not a reporter.");
        }

        _logger.LogDebug("Submitting incident for user {UserId} with priority {Priority}", userId, command.IncidentPriority);

        if(!Enum.TryParse<IncidentPriority>(command.IncidentPriority, out var priority)){
            _logger.LogWarning("User with id {UserId} entered invalid incident priority: {Priority}", userId, command.IncidentPriority);
            throw new ArgumentException("Invalid incident priority");
        } 
        var incident = new Domain.Entities.Incident
        {
            Title = command.Title,
            Description = command.Description,
            Priority = priority,
            ReportedBy = userId,
            Status = IncidentStatus.Open,
            CreatedAt = DateTime.UtcNow
        };
        _dbContext.Incidents.Add(incident);
        
        var auditEntry = new AuditEntry
        {
            Action = AuditEntryAction.Created,
            EntityName = nameof(Domain.Entities.Incident),
            PerformedBy = userId,
            Timestamp = DateTime.UtcNow
        };

        _dbContext.AuditEntries.Add(auditEntry);
        
        await _dbContext.SaveChangesAsync();

       _logger.LogInformation("Incident with id {Id} has been created by user {UserId}", incident.Id, userId);

       var reporter = await _dbContext.Users.FindAsync(incident.ReportedBy);
       if(reporter is null)
       {
            _logger.LogError("Reporter user with Id {ReporterId} not found for incident {IncidentId}.", incident.ReportedBy, incident.Id);
            throw new NotFoundException($"Reporter with Id {incident.ReportedBy} not found.");
       }

        BackgroundJob.Enqueue<SendEmailJob>(job => job.Execute(userId, reporter.Email, "New Incident Submitted", $"A new incident has been submitted by user {userId}."));

        return new IncidentResponse
        {
            Id = incident.Id,
            Title = incident.Title,
            Description = incident.Description,
            IncidentPriority = incident.Priority.ToString()
        };
    }

}

public class IncidentResponse
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string IncidentPriority { get; set; } = string.Empty;
}