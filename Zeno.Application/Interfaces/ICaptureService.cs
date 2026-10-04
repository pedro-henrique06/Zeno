using Zeno.Application.Requests.Capture;
using Zeno.Application.Responses.Capture;

namespace Zeno.Application.Interfaces;

public interface ICaptureService
{
    Task<CaptureKeyStatusResponse> GetKeyStatusAsync(Guid userId);

    /// <summary>Gera (ou substitui) a chave do usuário e devolve o texto uma única vez.</summary>
    Task<CaptureKeyResponse> CreateKeyAsync(Guid userId);

    Task RevokeKeyAsync(Guid userId);

    Task<IReadOnlyList<CaptureRuleResponse>> GetRulesAsync(Guid userId);
    Task<CaptureRuleResponse> AddRuleAsync(Guid userId, AddCaptureRuleRequest request);
    Task DeleteRuleAsync(Guid userId, Guid id);

    /// <summary>Cria o lançamento do dono da chave; null se a chave for inválida ou revogada.</summary>
    Task<CaptureEntryResponse?> CaptureAsync(string? key, CaptureEntryRequest request, string? timeZoneId);
}
