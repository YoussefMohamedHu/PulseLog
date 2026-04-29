using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PulseLog.Api.Domain.Entities;
using PulseLog.Api.Infrastructure.Persistence;
using PulseLog.Api.Domain.ValueObjects;
using PulseLog.Api.Features.Common.Exceptions;

namespace PulseLog.Api.Features.Auth.Register;

public class RegisterCommandHandler : IRequestHandler<RegisterCommand, RegisterResult>
{
    private readonly AppDbContext _dbContext;
    private readonly AuthService _authService;
    private readonly ILogger<RegisterCommandHandler> _logger;

    public RegisterCommandHandler(AppDbContext dbContext, AuthService authService,ILogger<RegisterCommandHandler> logger)
    {
        _logger = logger;
        _dbContext = dbContext;
        _authService = authService;
    }

    public async Task<RegisterResult> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        var existingUser = await _dbContext.Users.FirstOrDefaultAsync(u => u.Email == request.Email, cancellationToken);
        if (existingUser != null)
        {
            _logger.LogWarning("Registration attempt with existing email: {Email}", request.Email);
            
            throw new ConflictException("Email already exists");
        }

        var passwordHash = _authService.HashPassword(request.Password);

        var user = new User
        {
            Email = request.Email,
            FullName = request.FullName,
            Role = UserRole.Reporter,
            PasswordHash = passwordHash
        };

        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("User registered successfully: {Email}", user.Email);

        return new RegisterResult(user.Id, user.Email, user.FullName, user.Role.ToString());
    }
}
