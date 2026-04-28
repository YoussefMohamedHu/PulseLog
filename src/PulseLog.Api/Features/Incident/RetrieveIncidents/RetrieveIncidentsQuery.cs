using MediatR;
using PulseLog.Api.Features.Common.Models;

namespace PulseLog.Api.Features.Incident.RetrieveIncidents;

public record RetrieveIncidentsQuery(
    int PageNumber = 0,
    int PageSize = 20,
    string? IncidentPriority = null,
    string? IncidentStatus = null
) : IRequest<PagedResult<List<RetrieveIncidentsResponse>>>;