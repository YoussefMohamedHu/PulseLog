using MediatR;
using PulseLog.Api.Domain.ValueObjects;

namespace PulseLog.Api.Features.Auth.Login;

public record LoginQuery(string Email, string Password) : IRequest<LoginResult>;

public record LoginResult(string Token, int UserId, string Email, string FullName, UserRole Role);
