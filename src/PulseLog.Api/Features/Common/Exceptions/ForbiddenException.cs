namespace PulseLog.Api.Features.Common.Exceptions;

public class ForbiddenException : Exception
{
    public ForbiddenException(string message) : base(message) { }

    public ForbiddenException() : base("You do not have permission to access this resource.") { }
}
