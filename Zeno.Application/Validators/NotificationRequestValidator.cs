using FluentValidation;
using Zeno.Application.Requests.Notifications;

namespace Zeno.Application.Validators;

public class RegisterDeviceRequestValidator : AbstractValidator<RegisterDeviceRequest>
{
    public RegisterDeviceRequestValidator()
    {
        RuleFor(x => x.Token).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Platform).IsInEnum();
    }
}

public class UpdateNotificationPreferenceRequestValidator : AbstractValidator<UpdateNotificationPreferenceRequest>
{
    public UpdateNotificationPreferenceRequestValidator()
    {
        RuleFor(x => x.SendHour)
            .InclusiveBetween(0, 23)
            .WithMessage("A hora de envio deve estar entre 0 e 23.");

        RuleFor(x => x.TimeZoneId)
            .NotEmpty()
            .MaximumLength(100)
            .Must(BeAKnownTimeZone)
            .WithMessage("Fuso horario invalido.");
    }

    private static bool BeAKnownTimeZone(string timeZoneId)
    {
        if (string.IsNullOrWhiteSpace(timeZoneId))
            return false;

        return TimeZoneInfo.TryFindSystemTimeZoneById(timeZoneId, out _);
    }
}
