using FluentValidation;

namespace PulseLog.Api.Features.Incident.SubmitIncident;

public class SubmitIncidentCommandValidator : AbstractValidator<SubmitIncidentCommand>
{
    public SubmitIncidentCommandValidator()
    {
        RuleFor(x => x.Title).NotEmpty().WithMessage("Title is required");
        RuleFor(x => x.Description).NotEmpty().WithMessage("Description is required");
        RuleFor(x => x.IncidentPriority).NotEmpty().WithMessage("Incident Priorty is required");
    }
}