using MediatR;

namespace PulseLog.Api.Features.Incident.SubmitIncident;
public record SubmitIncidentCommand(string Title, string Description, string IncidentPriority) : IRequest<IncidentResponse>;