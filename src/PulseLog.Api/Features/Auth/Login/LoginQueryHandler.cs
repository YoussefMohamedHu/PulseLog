using MediatR;
using Microsoft.EntityFrameworkCore;
using PulseLog.Api.Domain.Entities;
using PulseLog.Api.Infrastructure.Persistence;

namespace PulseLog.Api.Features.Auth.Login;

public class LoginQueryHandler : IRequestHandler<LoginQuery, LoginResult>
{
    private readonly AppDbContext _dbContext;
    private readonly AuthService _authService;
    private readonly ILogger _logger;

    public LoginQueryHandler(AppDbContext dbContext, AuthService authService,ILogger logger)
    {
        _logger = logger;
        _dbContext = dbContext;
        _authService = authService;
    }

    public async Task<LoginResult> Handle(LoginQuery request, CancellationToken cancellationToken)
    {
        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Email == request.Email, cancellationToken);
        if (user == null || !_authService.ValidatePassword(request.Password, user.PasswordHash))
        {
            _logger.LogWarning("Invalid login attempt for email: {Email}", request.Email);

            throw new UnauthorizedAccessException("Invalid credentials");
        }

        var token = _authService.GenerateJwt(user.Id, user.Role.ToString());

        _logger.LogInformation("User logged in successfully: {Email}",user.Email);

        return new LoginResult(token, user.Id, user.Email, user.FullName, user.Role);
    }
}
