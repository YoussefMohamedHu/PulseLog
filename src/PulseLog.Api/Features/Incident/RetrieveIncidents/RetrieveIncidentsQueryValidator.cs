using FluentValidation;

namespace PulseLog.Api.Features.Incident.RetrieveIncidents;

public class RetrieveIncidentsQueryValidator : AbstractValidator<RetrieveIncidentsQuery>
{
    public RetrieveIncidentsQueryValidator()
    {
        RuleFor(x => x.PageNumber).GreaterThanOrEqualTo(0).WithMessage("Page number must be greater than or equal to 0");
        RuleFor(x => x.PageSize).GreaterThan(0).WithMessage("Page size must be greater than 0");
    }
}
