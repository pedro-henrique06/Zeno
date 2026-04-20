using SalaryEntity = Zeno.Domain.Salary.Salary;

namespace Zeno.Domain.Interfaces;

public interface ISalaryRepository
{
    Task<SalaryEntity?> GetByIdAsync(Guid id);
    Task<IEnumerable<SalaryEntity>> GetByWalletAsync(Guid walletId);
    Task<IEnumerable<SalaryEntity>> GetPendingSalariesAsync(int dayOfMonth);
    Task<SalaryEntity> CreateAsync(SalaryEntity salary);
    Task<SalaryEntity> UpdateAsync(SalaryEntity salary);
    Task DeleteAsync(Guid id);
    Task MarkProcessedAsync(Guid id);
}
