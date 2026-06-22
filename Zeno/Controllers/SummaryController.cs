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
    private readonly IValidator<YearQuery> _yearValidator;

    public SummaryController(ISummaryService service, IValidator<MonthQuery> validator, IValidator<YearQuery> yearValidator)
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

                return await _service.GetMonthlySummary(userId, query.Month, query.Year);
            },
            result => Ok(ApiResponse<SummaryResponse>.Ok(result)));
    }

    [HttpGet("horizon")]
    public async Task<IActionResult> GetEconomizedHorizon([FromQuery] YearQuery query)
    {
        var userId = GetUserId();
        return await HandleAsync(
            async () =>
            {
                var validation = await _yearValidator.ValidateAsync(query);
                if (!validation.IsValid)
                    throw new AppValidationException(validation);

                return await _service.GetEconomizedHorizon(userId, query.Year);
            },
            result => Ok(ApiResponse<EconomizedHorizonResponse>.Ok(result)));
    }

    [HttpGet("performance-horizon")]
    public async Task<IActionResult> GetPerformanceHorizon([FromQuery] YearQuery query)
    {
        var userId = GetUserId();
        return await HandleAsync(
            async () =>
            {
                var validation = await _yearValidator.ValidateAsync(query);
                if (!validation.IsValid)
                    throw new AppValidationException(validation);

                return await _service.GetPerformanceHorizon(userId, query.Year);
            },
            result => Ok(ApiResponse<PerformanceHorizonResponse>.Ok(result)));
    }

    [HttpGet("cost-of-living-horizon")]
    public async Task<IActionResult> GetCostOfLivingHorizon([FromQuery] YearQuery query)
    {
        var userId = GetUserId();
        return await HandleAsync(
            async () =>
            {
                var validation = await _yearValidator.ValidateAsync(query);
                if (!validation.IsValid)
                    throw new AppValidationException(validation);

                return await _service.GetCostOfLivingHorizon(userId, query.Year);
            },
            result => Ok(ApiResponse<CostOfLivingHorizonResponse>.Ok(result)));
    }
}
