using SalaryEntity = Zeno.Domain.Salary.Salary;

namespace Zeno.Domain.Interfaces;

public interface ISalaryRepository
{
    Task<SalaryEntity?> GetByIdAsync(Guid id);
    Task<SalaryEntity?> GetByIdAndUserAsync(Guid id, Guid userId);
    Task<IEnumerable<SalaryEntity>> GetByAccountAsync(Guid accountId);
    Task<IEnumerable<SalaryEntity>> GetByUserAsync(Guid userId);
    Task<IEnumerable<SalaryEntity>> GetPendingSalariesAsync(int dayOfMonth);
    Task<SalaryEntity> CreateAsync(SalaryEntity salary);
    Task<SalaryEntity> UpdateAsync(SalaryEntity salary);
    Task DeleteAsync(Guid id);
    Task MarkProcessedAsync(Guid id);
}