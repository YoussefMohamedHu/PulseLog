using PulseLog.Api.Domain.ValueObjects;

namespace PulseLog.Api.Features.Common.Abstractions;

public interface ICurrentUser
{
    public int GetCurrentUserId();
    public UserRole GetCurrentUserRole();
}