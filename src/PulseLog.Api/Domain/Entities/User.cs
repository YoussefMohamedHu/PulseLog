using PulseLog.Api.Domain.ValueObjects;

namespace PulseLog.Api.Domain.Entities;

/// <summary>
/// Represents a user in the system.
/// </summary>
public class User
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public string PasswordHash { get; set; } = string.Empty;
}


