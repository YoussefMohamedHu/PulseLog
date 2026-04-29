using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using PulseLog.Api.Features.Common.Abstractions;

namespace PulseLog.Api.Features.Incident.UpdateIncidentStatus;

public class UpdateIncidentStatusEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("/api/incidents/{id}/status", async (int id, UpdateIncidentStatusRequest request, ISender sender) =>
        {
            var command = new UpdateIncidentStatusCommand(id, request.Status);
            var response = await sender.Send(command);
            return Results.Ok(response);
        })
        .WithTags("Incidents")
        .WithDescription("Update the status of an incident")
        .RequireAuthorization()
        .Produces<UpdateIncidentStatusResponse>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden)
        .Produces(StatusCodes.Status404NotFound)
        .ProducesValidationProblem(StatusCodes.Status400BadRequest);
    }
}

public record UpdateIncidentStatusRequest(string Status);
