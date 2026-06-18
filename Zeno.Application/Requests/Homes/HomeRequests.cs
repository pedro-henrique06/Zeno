using Zeno.Domain.Enum;

namespace Zeno.Application.Requests.Homes;

public sealed class CreateHomeRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public SplitMode SplitMode { get; set; }
}

public sealed class UpdateHomeRequest
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public SplitMode SplitMode { get; set; }
}

public sealed class CreateHomeExpenseRequest
{
    public Guid HomeId { get; set; }
    public string Title { get; set; } = string.Empty;
    public decimal Value { get; set; }
    public Category Category { get; set; }
    public int Month { get; set; }
    public int Year { get; set; }
}

public sealed class AddHomeMemberRequest
{
    public Guid UserId { get; set; }
}

public sealed class UpdateHomeMemberSalaryRequest
{
    public Guid UserId { get; set; }
    public decimal Salary { get; set; }
}