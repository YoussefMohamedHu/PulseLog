using MediatR;
using Microsoft.EntityFrameworkCore;
using PulseLog.Api.Domain.Entities;
using PulseLog.Api.Domain.ValueObjects;
using PulseLog.Api.Features.Common.Abstractions;
using PulseLog.Api.Features.Common.Exceptions;
using PulseLog.Api.Infrastructure.Jobs;
using PulseLog.Api.Infrastructure.Persistence;
using Hangfire;

namespace PulseLog.Api.Features.Incident.UpdateIncidentStatus;

public class UpdateIncidentStatusCommandHandler : IRequestHandler<UpdateIncidentStatusCommand, UpdateIncidentStatusResponse>
{
    private readonly AppDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<UpdateIncidentStatusCommandHandler> _logger;

    public UpdateIncidentStatusCommandHandler(
        AppDbContext dbContext,
        ICurrentUser currentUser,
        ILogger<UpdateIncidentStatusCommandHandler> logger)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<UpdateIncidentStatusResponse> Handle(UpdateIncidentStatusCommand command, CancellationToken ct)
    {
        var userId = _currentUser.GetCurrentUserId();
        var userRole = _currentUser.GetCurrentUserRole();

        if (userRole != UserRole.Agent && userRole != UserRole.Admin)
        {
            _logger.LogWarning("User {UserId} attempted to update incident status without proper permissions.", userId);
            throw new UnauthorizedAccessException("Only agents and admins can update incident status.");
        }

        _logger.LogDebug("User {UserId} is updating status of incident {IncidentId} to {NewStatus}.", userId, command.IncidentId, command.NewStatus);

        var incident = await _dbContext.Incidents.FindAsync(command.IncidentId);
        if (incident == null)
        {
            _logger.LogWarning("Incident {IncidentId} not found.", command.IncidentId);
            throw new NotFoundException($"Incident with Id {command.IncidentId} not found.");
        }

        if (!Enum.TryParse<IncidentStatus>(command.NewStatus, out var newStatus))
        {
            _logger.LogWarning("Invalid incident status: {Status}", command.NewStatus);
            throw new ArgumentException($"Invalid incident status: {command.NewStatus}");
        }

        if (!incident.UpdateStatus(newStatus))
        {
            _logger.LogWarning(
                "Incident {IncidentId} cannot transition from {CurrentStatus} to {NewStatus}.",
                incident.Id,
                incident.Status,
                newStatus);
            throw new ConflictException($"Incident status cannot transition from {incident.Status} to {newStatus}.");
        }

        var auditEntry = new AuditEntry
        {
            Action = AuditEntryAction.Updated,
            EntityName = nameof(Incident),
            PerformedBy = userId,
            Timestamp = DateTime.UtcNow
        };

        _dbContext.AuditEntries.Add(auditEntry);
        await _dbContext.SaveChangesAsync(ct);

        _logger.LogInformation("Incident {IncidentId} status updated to {Status} by user {UserId}.", incident.Id, newStatus, userId);

        var reporter = await _dbContext.Users.FindAsync(incident.ReportedBy);
        if (reporter != null)
        {
            BackgroundJob.Enqueue<SendEmailJob>(job => job.Execute(
                reporter.Id, reporter.Email,
                "Incident Status Updated",
                $"Your incident #{incident.Id} status has been updated to {newStatus}."));
        }

        return new UpdateIncidentStatusResponse
        {
            Id = incident.Id,
            Title = incident.Title,
            Description = incident.Description,
            Priority = incident.Priority.ToString(),
            Status = incident.Status.ToString(),
            AssignedTo = incident.AssignedTo,
            ReportedBy = incident.ReportedBy,
            CreatedAt = incident.CreatedAt,
            ResolvedAt = incident.ResolvedAt
        };
    }
}

public class UpdateIncidentStatusResponse
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int? AssignedTo { get; set; }
    public int ReportedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
}
