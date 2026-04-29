using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using PulseLog.Api.Features.Common.Abstractions;

namespace PulseLog.Api.Features.Incident.AssignIncident;

public class AssignIncidentEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("/api/incidents/{id}/assign", async (int id, ISender sender) =>
        {
            var command = new AssignIncidentCommand(id);
            var response = await sender.Send(command);
            return Results.Ok(response);
        })
        .WithTags("Incidents")
        .WithDescription("Assign an incident to the current agent")
        .RequireAuthorization()
        .Produces<AssignIncidentResponse>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden)
        .Produces(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status409Conflict)
        .ProducesValidationProblem(StatusCodes.Status400BadRequest);
    }
}
