using Microsoft.AspNetCore.Mvc;
using Zeno.Application.Interfaces;
using Zeno.Application.Requests;

namespace Zeno.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProjectionController : AppControllerBase
{
    private readonly IProjectionService _projectionService;

    public ProjectionController(IProjectionService projectionService)
    {
        _projectionService = projectionService;
    }

    [HttpPost("simulate")]
    public Task<IActionResult> Simulate([FromBody] ProjectionSimulationRequest request)
    {
        var userId = GetUserId();
        return HandleAsync(() => _projectionService.SimulateAsync(userId, request), data => Ok(data));
    }
}
