namespace PulseLog.Api.Domain.Entities;

using PulseLog.Api.Domain.ValueObjects;

/// <summary>
/// Represents an audit entry for tracking entity changes.
/// </summary>
public class AuditEntry
{
    public int Id { get; set; }
    public string EntityName { get; set; } = string.Empty;
    public AuditEntryActionEnum Action { get; set; }
    public int PerformedBy { get; set; }
    public DateTime Timestamp { get; set; }
}
