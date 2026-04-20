using Microsoft.AspNetCore.Mvc;
using Zeno.Application.Exceptions;
using Zeno.Responses;

namespace Zeno.Controllers;

public abstract class AppControllerBase : ControllerBase
{
    protected async Task<IActionResult> HandleAsync<T>(Func<Task<T>> action, Func<T, IActionResult> onSuccess)
    {
        try
        {
            var data = await action();
            return onSuccess(data);
        }
        catch (AppValidationException ex)
        {
            var errors = ex.ValidationResult.Errors.Select(e => new ValidationError
            {
                Property = e.PropertyName,
                Error = e.ErrorMessage
            });

            return BadRequest(errors);
        }
    }
}
