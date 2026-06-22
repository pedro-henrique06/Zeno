using Zeno.Domain.Enum;

namespace Zeno.Application.Requests;

public class UpdateCurrencyRequest
{
    public Currency Currency { get; set; }
}
