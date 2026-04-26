namespace PulseLog.Api.Domain.ValueObjects;

/// <summary>
/// Represents the status of an incident.
/// </summary>
public enum IncidentStatus
{
    Open = 0,
    InProgress = 1,
    Resolved = 2,
    Closed = 3
}
