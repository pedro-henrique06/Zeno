using Zeno.Application.Responses.Widgets;

namespace Zeno.Application.Interfaces;

public interface IWidgetService
{
    Task<WidgetKeyStatusResponse> GetKeyStatusAsync(Guid userId);

    /// <summary>Gera (ou substitui) a chave do usuário e devolve o texto uma única vez.</summary>
    Task<WidgetKeyResponse> CreateKeyAsync(Guid userId);

    Task RevokeKeyAsync(Guid userId);

    /// <summary>Resumo do usuário dono da chave; null se a chave for inválida ou revogada.</summary>
    Task<WidgetSummaryResponse?> GetSummaryAsync(string? key, string? timeZoneId);
}
