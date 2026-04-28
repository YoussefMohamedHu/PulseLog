namespace PulseLog.Api.Features.Incident.RetrieveIncidents;

public record RetrieveIncidentsResponse(
    int Id,
    string Title,
    string Description,
    string Priority,
    string Status,
    int ReportedBy,
    int? AssignedTo,
    DateTime? AssignedAt,
    DateTime CreatedAt);
