using MediatR;
using PulseLog.Api.Features.Common.Abstractions;

namespace PulseLog.Api.Features.Incident.SubmitIncident;

public class SubmitIncidentEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/incidents/", async (SubmitIncidentCommand command, ISender sender) =>
        {
            var response = await sender.Send(command);
            return Results.Ok(response);
        })
        .WithTags("Incidents")
        .WithDescription("Submit new incident by a reporter")
        .Produces(StatusCodes.Status201Created)
        .Produces(StatusCodes.Status401Unauthorized)
        .ProducesValidationProblem(StatusCodes.Status400BadRequest);
    }
}