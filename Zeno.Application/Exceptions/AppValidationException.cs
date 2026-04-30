using FluentValidation.Results;

namespace Zeno.Application.Exceptions;

public class AppValidationException : Exception
{
    public ValidationResult ValidationResult { get; }

    public AppValidationException(ValidationResult validationResult)
        : base("One or more validation errors occurred.")
    {
        ValidationResult = validationResult;
    }
}