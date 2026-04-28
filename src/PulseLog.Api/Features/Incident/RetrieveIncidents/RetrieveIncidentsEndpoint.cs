using MediatR;
using PulseLog.Api.Features.Common.Abstractions;
using PulseLog.Api.Features.Common.Models;

namespace PulseLog.Api.Features.Incident.RetrieveIncidents;

public class RetrieveIncidentsIncident : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/incidents", async (ISender sender, [AsParameters] RetrieveIncidentsQuery query) =>
        {
            var result = await sender.Send(query);
            return Results.Ok(result);
        })
        .WithTags("Incidents")
        .WithDescription(
            "Retrieve a list of paginated incidents that match the specified criteria" +
        " which can be filtered by priority and status.")
        .Produces<PagedResult<List<RetrieveIncidentsResponse>>>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status401Unauthorized);
        
    }
}