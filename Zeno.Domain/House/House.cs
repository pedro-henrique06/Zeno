namespace Zeno.Domain.House;

public class House
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public List<HouseMember> Members { get; set; } = new();

    /// <summary>Meta da casa (vazio = sem meta). Alimentada pela fatia de 20% do 50/30/20.</summary>
    public string GoalName { get; set; } = string.Empty;

    public decimal? GoalTargetAmount { get; set; }

    /// <summary>Os meses a partir deste contam para o progresso da meta.</summary>
    public DateTime? GoalStartDate { get; set; }
}
