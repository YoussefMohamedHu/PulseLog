using MediatR;

namespace PulseLog.Api.Features.Incident.AssignIncident;

public record AssignIncidentCommand(int IncidentId) : IRequest<AssignIncidentResponse>;
