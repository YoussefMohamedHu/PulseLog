using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace PulseLog.Api.Features.Common.Abstractions;

public interface IEndpoint
{
    void MapEndpoint(IEndpointRouteBuilder app);
}
