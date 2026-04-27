using MediatR;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using PulseLog.Api.Features.Common.Abstractions;
using PulseLog.Api.Features.Common.Behaviors;
using PulseLog.Api.Infrastructure.Persistence;
using System.Reflection;
using PulseLog.Api.Features.Common.Middlewares;
using PulseLog.Api.Features.Auth;
using FluentValidation.AspNetCore;
using PulseLog.Api.Features.Common.Configurations;
using PulseLog.Api.Infrastructure;
using Hangfire;
using Scalar.AspNetCore;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {CorrelationId} {Message:lj}{NewLine}{Exception}")
    .WriteTo.File("logs/pulselog-.log", rollingInterval: RollingInterval.Day,
        outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {CorrelationId} {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog();

    builder.Services.AddApplication();
    builder.Services.AddInfrastructure(builder.Configuration);

    builder.Services.AddOpenApi();

    var app = builder.Build();

    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
        app.MapScalarApiReference();
    }

    app.UseHangfireDashboard(builder.Configuration["Hangfire:DashboardPath"] ?? "/hangfire");

    app.UseMiddleware<LoggerCorrelationId>();

    app.UseSerilogRequestLogging();

    app.UseAuthentication();
    app.UseAuthorization();

    app.RegisterEndpoints();
    if(app.Environment.IsDevelopment()){
        using (var scope = app.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        }
    }

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
