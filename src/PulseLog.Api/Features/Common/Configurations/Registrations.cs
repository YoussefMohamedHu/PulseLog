using System.Reflection;
using FluentValidation;
using FluentValidation.AspNetCore;
using MediatR;
using PulseLog.Api.Features.Common.Abstractions;
using PulseLog.Api.Features.Auth;
using PulseLog.Api.Features.Common.Behaviors;

namespace PulseLog.Api.Features.Common.Configurations;

public static class Registrations
{
    public static void RegisterEndpoints(this WebApplication app)
    {
        var endpointTypes = Assembly.GetExecutingAssembly().GetTypes()
        .Where(t => typeof(IEndpoint).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);

        foreach(var endpointType in endpointTypes)
        {
            var endpoint = (IEndpoint)ActivatorUtilities.CreateInstance(app.Services, endpointType)!;
            endpoint.MapEndpoint(app);
        }
    }
    public static void AddApplication(this IServiceCollection services)
    {
        #region Features Services
        services.AddScoped<AuthService>();
        #endregion

        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        services.AddFluentValidationAutoValidation();
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
    }
    
}