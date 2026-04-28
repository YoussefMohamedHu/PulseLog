using System.Net;
using System.Text.Json;
using FluentValidation;
using PulseLog.Api.Features.Common.Exceptions;

namespace PulseLog.Api.Features.Common.Middlewares;

public class GlobalExceptionHandlerMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandlerMiddleware> _logger;

    public GlobalExceptionHandlerMiddleware(RequestDelegate next, ILogger<GlobalExceptionHandlerMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "An expected exception occurred: {ExceptionType}", ex.GetType().Name);
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var (statusCode, message) = exception switch
        {
            ValidationException validationEx =>
                (HttpStatusCode.BadRequest, FormatValidationErrors(validationEx)),

            ArgumentException argumentEx =>
                (HttpStatusCode.BadRequest, argumentEx.Message),

            NotFoundException notFoundEx =>
                (HttpStatusCode.NotFound, notFoundEx.Message),

            UnauthorizedAccessException unauthorizedEx =>
                (HttpStatusCode.Unauthorized, unauthorizedEx.Message),

            ForbiddenException forbiddenEx =>
                (HttpStatusCode.Forbidden, forbiddenEx.Message),

            ConflictException conflictEx =>
                (HttpStatusCode.Conflict, conflictEx.Message),

            InvalidOperationException invalidOpEx =>
                (HttpStatusCode.Conflict, invalidOpEx.Message),

            NotSupportedException notSupportedEx =>
                (HttpStatusCode.BadRequest, notSupportedEx.Message),

            OperationCanceledException =>
                (HttpStatusCode.ServiceUnavailable, "The operation was canceled."),

            TimeoutException =>
                (HttpStatusCode.GatewayTimeout, "The operation timed out."),

            _ =>
                (HttpStatusCode.InternalServerError, "An unexpected error occurred. Please try again later.")
        };

        if (statusCode == HttpStatusCode.InternalServerError)
        {
            _logger.LogError(exception, "Unhandled exception");
        }

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;

        var response = new
        {
            StatusCode = (int)statusCode,
            Message = message
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        }));
    }

    private static string FormatValidationErrors(ValidationException validationEx)
    {
        var errors = validationEx.Errors
            .GroupBy(e => e.PropertyName)
            .ToDictionary(
                g => g.Key,
                g => g.Select(e => e.ErrorMessage).ToArray());

        return JsonSerializer.Serialize(errors, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
    }
}
