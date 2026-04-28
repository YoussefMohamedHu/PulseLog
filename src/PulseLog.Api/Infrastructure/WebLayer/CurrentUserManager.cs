using System.Security.Claims;
using PulseLog.Api.Domain.ValueObjects;
using PulseLog.Api.Features.Common.Abstractions;

namespace PulseLog.Api.Infrastructure.WebLayer;

public class CurrentUserManager(IHttpContextAccessor httpContextAccessor, ILogger<CurrentUserManager> logger) : ICurrentUser
{
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;
    private readonly ILogger<CurrentUserManager> _logger = logger;
    public int GetCurrentUserId()
    {
        _logger.LogDebug("Getting current user id");

        var userId = _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
        {
            _logger.LogWarning("User is not authenticated");

            throw new UnauthorizedAccessException("User is not authenticated.");
        }
        return int.Parse(userId);
    }

    public UserRole GetCurrentUserRole()
    {
        _logger.LogDebug("Getting current user role");
        
        var userRole = _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.Role);
        if (userRole is null)
        {
            _logger.LogWarning("User is not authenticated");

            throw new UnauthorizedAccessException("User is not authenticated.");
        }
        return Enum.Parse<UserRole>(userRole);
    }
}