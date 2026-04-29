using FluentValidation;

namespace PulseLog.Api.Features.Incident.AssignIncident;

public class AssignIncidentCommandValidator : AbstractValidator<AssignIncidentCommand>
{
    public AssignIncidentCommandValidator()
    {
        RuleFor(x => x.IncidentId).GreaterThan(0).WithMessage("Valid incident ID is required");
    }
}
