namespace PulseLog.Api.Domain.Entities;

using PulseLog.Api.Domain.ValueObjects;

/// <summary>
/// Represents an incident reported in the system.
/// </summary>
public class Incident
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int ReportedBy { get; set; } 
    public int? AssignedTo { get; set; } = null;
    public IncidentPriority Priority { get; set; }
    public IncidentStatus Status { get; set; }
    public DateTime CreatedAt { get; set; } 
    public DateTime? ResolvedAt { get; set; } = null;
    public DateTime? AssignedAt { get; set; } = null;
    public int ReassignmentCount { get; set; } = 0;

    public bool UpdateStatus(IncidentStatus newStatus)
    {
        if (!GetValidTransitionsMatrix().TryGetValue(Status, out var validTransitions))
        {
            return false;
        }

        if (Array.IndexOf(validTransitions, newStatus) < 0)
        {
            return false;
        }

        Status = newStatus;

        if (newStatus == IncidentStatus.Resolved)
        {
            ResolvedAt = DateTime.UtcNow;
        }
        else if (newStatus == IncidentStatus.Open)
        {
            ResolvedAt = null;
        }

        return true;
    }

    private static IReadOnlyDictionary<IncidentStatus, IncidentStatus[]> GetValidTransitionsMatrix()
    {
        return new Dictionary<IncidentStatus, IncidentStatus[]>
        {
            [IncidentStatus.Open] = [IncidentStatus.InProgress],
            [IncidentStatus.InProgress] = [IncidentStatus.Open, IncidentStatus.Resolved],
            [IncidentStatus.Resolved] = [IncidentStatus.Open, IncidentStatus.Closed],
            [IncidentStatus.Closed] = [IncidentStatus.Open]
        };
    }
}


