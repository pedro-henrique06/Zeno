using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Zeno.Application.Exceptions;
using Zeno.Application.Interfaces;
using Zeno.Application.Requests;
using Zeno.Application.Responses.Balances;
using Zeno.Application.Responses.Common;

namespace Zeno.Controllers;

[ApiController]
[Route("api/balances")]
public class BalanceController : AppControllerBase
{
    private readonly IBalanceService _service;
    private readonly IValidator<MonthQuery> _validator;
    private readonly IValidator<YearQuery> _yearValidator;

    public BalanceController(IBalanceService service, IValidator<MonthQuery> validator, IValidator<YearQuery> yearValidator)
    {
        _service = service;
        _validator = validator;
        _yearValidator = yearValidator;
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

                return await _service.GetMonthlyBalances(userId, query.Month, query.Year);
            },
            result => Ok(ApiResponse<BalancesResponse>.Ok(result)));
    }

    [HttpGet("horizon")]
    public async Task<IActionResult> GetHorizon([FromQuery] YearQuery query)
    {
        var userId = GetUserId();
        return await HandleAsync(
            async () =>
            {
                var validation = await _yearValidator.ValidateAsync(query);
                if (!validation.IsValid)
                    throw new AppValidationException(validation);

                return await _service.GetYearlyBalances(userId, query.Year);
            },
            result => Ok(ApiResponse<BalancesHorizonResponse>.Ok(result)));
    }
}
