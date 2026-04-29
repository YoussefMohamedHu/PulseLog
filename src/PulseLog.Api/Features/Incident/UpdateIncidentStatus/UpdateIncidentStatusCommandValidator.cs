using FluentValidation;

namespace PulseLog.Api.Features.Incident.UpdateIncidentStatus;

public class UpdateIncidentStatusCommandValidator : AbstractValidator<UpdateIncidentStatusCommand>
{
    public UpdateIncidentStatusCommandValidator()
    {
        RuleFor(x => x.IncidentId).GreaterThan(0).WithMessage("Valid incident ID is required");
        RuleFor(x => x.NewStatus).NotEmpty().WithMessage("New status is required");
    }
}
