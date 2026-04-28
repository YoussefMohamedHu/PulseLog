using MediatR;
using Microsoft.EntityFrameworkCore;
using PulseLog.Api.Features.Common.Abstractions;
using PulseLog.Api.Features.Common.Models;
using PulseLog.Api.Infrastructure.Persistence;
using PulseLog.Api.Domain.ValueObjects;

namespace PulseLog.Api.Features.Incident.RetrieveIncidents;

public class RetrieveIncidentsQueryHandler : IRequestHandler<RetrieveIncidentsQuery, PagedResult<List<RetrieveIncidentsResponse>>>
{
    private readonly ICurrentUser _currentUser;
    private readonly AppDbContext _dbContext;
    private readonly ILogger<RetrieveIncidentsQueryHandler> _logger;

    public RetrieveIncidentsQueryHandler(
        ICurrentUser currentUser,
        AppDbContext dbContext,
        ILogger<RetrieveIncidentsQueryHandler> logger)
    {
        _currentUser = currentUser;
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<PagedResult<List<RetrieveIncidentsResponse>>> Handle(RetrieveIncidentsQuery request, CancellationToken ct)
    {
        var userId = _currentUser.GetCurrentUserId();
        var userRole = _currentUser.GetCurrentUserRole();

        _logger.LogDebug("Retrieving incidents for user {UserId} with role {UserRole}", userId, userRole);

        IQueryable<Domain.Entities.Incident> query = userRole switch
        {
            UserRole.Reporter => _dbContext.Incidents.Where(i => i.ReportedBy == userId),
            UserRole.Agent or UserRole.Admin => _dbContext.Incidents,
            _ => throw new UnauthorizedAccessException($"User with role '{userRole}' is not authorized to retrieve incidents.")
        };

        if (!string.IsNullOrWhiteSpace(request.IncidentPriority))
        {
            if (Enum.TryParse<IncidentPriority>(request.IncidentPriority, true, out var priority))
            {
                query = query.Where(i => i.Priority == priority);
                _logger.LogDebug("Filtering incidents by priority: {Priority}", priority);
            }
            else
            {
                _logger.LogWarning("Invalid incident priority filter: {Priority}", request.IncidentPriority);
                throw new ArgumentException($"Invalid incident priority: {request.IncidentPriority}");
            }
        }

        if (!string.IsNullOrWhiteSpace(request.IncidentStatus))
        {
            if (Enum.TryParse<IncidentStatus>(request.IncidentStatus, true, out var status))
            {
                query = query.Where(i => i.Status == status);
                _logger.LogDebug("Filtering incidents by status: {Status}", status);
            }
            else
            {
                _logger.LogWarning("Invalid incident status filter: {Status}", request.IncidentStatus);
                throw new ArgumentException($"Invalid incident status: {request.IncidentStatus}");
            }
        }

        var totalCount = await query.CountAsync(ct);
        var totalPages = (int)Math.Ceiling(totalCount / (double)request.PageSize);

        var incidents = await query
            .Skip(request.PageNumber * request.PageSize)
            .Take(request.PageSize)
            .Select(i => new RetrieveIncidentsResponse(
                i.Id,
                i.Title,
                i.Description,
                i.Priority.ToString(),
                i.Status.ToString(),
                i.ReportedBy,
                i.AssignedTo,
                i.AssignedAt,
                i.CreatedAt
            ))
            .ToListAsync(ct);

        _logger.LogInformation("Retrieved {Count} incidents out of {TotalCount} total for user {UserId}", incidents.Count, totalCount, userId);

        return new PagedResult<List<RetrieveIncidentsResponse>>
        {
            Page = request.PageNumber,
            PageSize = request.PageSize,
            TotalCount = totalCount,
            TotalPages = totalPages,
            Value = incidents
        };
    }
}
