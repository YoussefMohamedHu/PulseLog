using PulseLog.Api.Domain.ValueObjects;
using PulseLog.Api.Features.Common.Abstractions.Services;

namespace PulseLog.Api.Infrastructure.Services;

public class EmailService : IEmailService
{
    public bool SendEmailToUser(int userId)
    {
        //TODO sending email to a single user
        throw new NotImplementedException();
    }

    public bool BroadcastEmailByUserRole(UserRole userRole)
    {
        //TODO sending email to all users within this role
        throw new NotImplementedException();
    }

}