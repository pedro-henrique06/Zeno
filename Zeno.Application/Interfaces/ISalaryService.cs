using Zeno.Domain.Salary;

namespace Zeno.Application.Interfaces;

public interface ISalaryService
{
    Task<Salary> CreateSalary(Salary salary);
    Task<Salary> UpdateSalary(Salary salary);
    Task<Salary> DeleteSalary(Guid id);
    Task<Salary?> GetSalaryById(Guid id);
    Task<IEnumerable<Salary>> GetSalariesByWallet(Guid walletId);
    Task<IEnumerable<Salary>> GetSalariesByUser(Guid userId);
    Task ProcessPendingSalaries();
}
