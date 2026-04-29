using MediatR;
using FluentValidation;
using PulseLog.Api.Features.Common.Abstractions;
using PulseLog.Api.Features.Auth.Register;

namespace PulseLog.Api.Features.Auth.Register;

public class RegisterEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/auth/register", async (RegisterCommand command, ISender mediator) =>
        {
            try
            {
                var result = await mediator.Send(command);
                
                return Results.Created($"/api/users/{result.Id}", result);
            }
            catch (ValidationException ex)
            {
                return Results.ValidationProblem(ex.Errors.ToDictionary(e => e.PropertyName, e => new[] { e.ErrorMessage }));
            }
            catch (InvalidOperationException ex) when (ex.Message == "Email already exists")
            {
                return Results.Conflict(new { Message = "Email already exists" });
            }
        })
        .Produces<RegisterResult>(StatusCodes.Status201Created)
        .ProducesValidationProblem(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status409Conflict)
        .WithTags("Authentication")
        .WithName("Register")
        .WithDescription("Registration endpoint for Reporters ONLY");
        
    }
}
