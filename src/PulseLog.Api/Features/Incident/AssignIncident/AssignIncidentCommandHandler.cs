using MediatR;
using Microsoft.EntityFrameworkCore;
using PulseLog.Api.Domain.Entities;
using PulseLog.Api.Domain.ValueObjects;
using PulseLog.Api.Features.Common.Abstractions;
using PulseLog.Api.Features.Common.Exceptions;
using PulseLog.Api.Infrastructure.Jobs;
using PulseLog.Api.Infrastructure.Persistence;
using Hangfire;

namespace PulseLog.Api.Features.Incident.AssignIncident;

public class AssignIncidentCommandHandler : IRequestHandler<AssignIncidentCommand, AssignIncidentResponse>
{
    private readonly AppDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<AssignIncidentCommandHandler> _logger;

    public AssignIncidentCommandHandler(
        AppDbContext dbContext,
        ICurrentUser currentUser,
        ILogger<AssignIncidentCommandHandler> logger)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<AssignIncidentResponse> Handle(AssignIncidentCommand command, CancellationToken ct)
    {
        var userId = _currentUser.GetCurrentUserId();
        var userRole = _currentUser.GetCurrentUserRole();

        if (userRole != UserRole.Agent && userRole != UserRole.Admin)
        {
            _logger.LogWarning("User with Id {UserId} attempted to assign incident with Id {IncidentId} without proper permissions.", userId, command.IncidentId);

            throw new UnauthorizedAccessException("Only agents and admins can assign incidents.");
        }
        
        _logger.LogDebug("User with Id {UserId} is assigning incident with Id {IncidentId}.", userId, command.IncidentId);

        var incident = await _dbContext.Incidents.FindAsync(command.IncidentId);
        if (incident == null)
        {   
            _logger.LogWarning("Incident with Id {IncidentId} not found.", command.IncidentId);

            throw new NotFoundException($"Incident with Id {command.IncidentId} not found.");
        }

        if (incident.AssignedTo != null)
        {
            _logger.LogWarning("Incident with Id {IncidentId} is already assigned.", command.IncidentId);

            throw new ConflictException($"Incident with Id {command.IncidentId} is already assigned.");
        }

        if (!incident.UpdateStatus(IncidentStatus.InProgress))
        {
            _logger.LogWarning(
                "Incident {IncidentId} cannot transition from {CurrentStatus} to {NewStatus}.",
                incident.Id,
                incident.Status,
                IncidentStatus.InProgress);
            throw new ConflictException($"Incident status cannot transition from {incident.Status} to {IncidentStatus.InProgress}.");
        }

        incident.AssignedTo = userId;
        incident.AssignedAt = DateTime.UtcNow;

        var auditEntry = new AuditEntry
        {
            EntityName = nameof(Domain.Entities.Incident),
            Action = AuditEntryAction.Updated,
            PerformedBy = userId,
            Timestamp = DateTime.UtcNow
        };

        _dbContext.AuditEntries.Add(auditEntry);

        await _dbContext.SaveChangesAsync(ct);

        var reporter = await _dbContext.Users.FindAsync(incident.ReportedBy);
        if (reporter is null)
        {
            _logger.LogError("Reporter user with Id {ReporterId} not found for incident {IncidentId}.", incident.ReportedBy, incident.Id);
            throw new NotFoundException($"Reporter with Id {incident.ReportedBy} not found.");
        }

        _logger.LogInformation("Incident with Id {IncidentId} assigned to user with Id {UserId} successfully.", command.IncidentId, userId);

        BackgroundJob.Enqueue<SendEmailJob>(job => job.Execute(
            reporter.Id, reporter.Email,
            "Incident Assigned",
            $"Your incident #{incident.Id} has been assigned to an agent."));

        var agents = await _dbContext.Users
            .Where(u => u.Role == UserRole.Agent)
            .ToListAsync(ct);

        if (agents.Count != 0)
        {
            BackgroundJob.Enqueue<BroadcastEmailJob>(job => job.Execute(
                agents.Select(a => a.Id),
                agents.Select(a => a.Email),
                "New Incident Assigned",
                $"Incident #{incident.Id} has been assigned. Please review."));
        }

        _logger.LogDebug("Email notifications enqueued for incident {IncidentId}.", command.IncidentId);

        return new AssignIncidentResponse(
            incident.Id,
            incident.Title,
            incident.Description,
            incident.Priority.ToString(),
            incident.Status.ToString(),
            incident.AssignedTo,
            incident.ReportedBy,
            reporter.FullName,
            incident.CreatedAt,
            incident.ResolvedAt
        );
    }
}

public record AssignIncidentResponse(
     int IncidentId,
     string Title,
     string Description ,
     string Priority,
     string Status,
     int? AssignedTo,
     int ReportedBy ,
     string ReporterName ,
     DateTime CreatedAt ,
     DateTime? ResolvedAt
);