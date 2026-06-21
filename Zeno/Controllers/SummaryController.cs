using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Zeno.Application.Exceptions;
using Zeno.Application.Interfaces;
using Zeno.Application.Requests;
using Zeno.Application.Responses.Common;
using Zeno.Application.Responses.Summary;

namespace Zeno.Controllers;

[ApiController]
[Route("api/summary")]
public class SummaryController : AppControllerBase
{
    private readonly ISummaryService _service;
    private readonly IValidator<MonthQuery> _validator;

    public SummaryController(ISummaryService service, IValidator<MonthQuery> validator)
    {
        _service = service;
        _validator = validator;
    }

    [HttpGet]
    public async Task<IActionResult> GetMonthly([FromQuery] MonthQuery query)
    {
        var userId = GetUserId();
        return await HandleAsync(
            async () =>
            {
                var validation = await _validator.ValidateAsync(query);
                if (!validation.IsValid)
                    throw new AppValidationException(validation);

                return await _service.GetMonthlySummary(userId, query.Month, query.Year);
            },
            result => Ok(ApiResponse<SummaryResponse>.Ok(result)));
    }
}
