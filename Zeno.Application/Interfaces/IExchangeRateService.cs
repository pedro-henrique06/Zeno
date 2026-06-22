using Zeno.Domain.Enum;

namespace Zeno.Application.Interfaces;

public interface IExchangeRateService
{
    Task<decimal> GetRateAsync(Currency from, Currency to);
}
