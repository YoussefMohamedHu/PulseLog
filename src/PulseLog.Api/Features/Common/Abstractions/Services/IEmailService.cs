using PulseLog.Api.Domain.ValueObjects;

namespace PulseLog.Api.Features.Common.Abstractions.Services;

public interface IEmailService
{
    public bool SendEmailToUser(int userId);
    public bool BroadcastEmailByUserRole(UserRole userRole);
}