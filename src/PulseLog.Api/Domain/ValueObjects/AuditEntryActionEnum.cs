namespace PulseLog.Api.Domain.ValueObjects;

/// <summary>
/// Represents the action performed in an audit entry.
/// </summary>
public enum AuditEntryAction
{
    Created = 0,
    Updated = 1,
    StatusChanged = 2
}
