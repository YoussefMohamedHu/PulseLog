using MediatR;

namespace PulseLog.Api.Features.Incident.UpdateIncidentStatus;

public record UpdateIncidentStatusCommand(int IncidentId, string NewStatus) : IRequest<UpdateIncidentStatusResponse>;
