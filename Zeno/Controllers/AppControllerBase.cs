using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Zeno.Application.Exceptions;
using Zeno.Responses;

namespace Zeno.Controllers;

public abstract class AppControllerBase : ControllerBase
{
    protected Guid GetUserId()
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub");
        return Guid.Parse(sub!);
    }

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

    protected async Task<IActionResult> HandleAsync(Func<Task> action, IActionResult onSuccess)
    {
        try
        {
            await action();
            return onSuccess;
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
