using Zeno.Domain.Capture;

namespace Zeno.Domain.Interfaces;

public interface ICaptureRuleRepository
{
    /// <summary>Regras do usuário da mais antiga para a mais nova (a primeira que casar vale).</summary>
    Task<IReadOnlyList<CaptureRule>> GetByUserAsync(Guid userId);
    Task<CaptureRule> CreateAsync(CaptureRule rule);

    /// <summary>Apaga a regra se ela pertencer ao usuário; devolve false se não existir.</summary>
    Task<bool> DeleteAsync(Guid userId, Guid id);
}
