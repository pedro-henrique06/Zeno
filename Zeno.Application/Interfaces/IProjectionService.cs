using Zeno.Application.Requests;
using Zeno.Application.Responses;

namespace Zeno.Application.Interfaces;

public interface IProjectionService
{
    Task<ProjectionResponse> SimulateAsync(Guid userId, ProjectionSimulationRequest request);
}
