using MediatR;
using PulseLog.Api.Domain.ValueObjects;

namespace PulseLog.Api.Features.Auth.Register;

public record RegisterCommand(string Email, string FullName, string Password) : IRequest<RegisterResult>;

public record RegisterResult(int Id, string Email, string FullName, string Role);
