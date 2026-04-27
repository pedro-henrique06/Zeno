using Zeno.Domain.Salary;

namespace Zeno.Application.Interfaces;

public interface ISalaryService
{
    Task<Salary> CreateSalary(Guid userId, Salary salary);
    Task<Salary> UpdateSalary(Guid userId, Salary salary);
    Task<Salary> DeleteSalary(Guid userId, Guid id);
    Task<Salary?> GetSalaryById(Guid userId, Guid id);
    Task<IEnumerable<Salary>> GetSalariesByWallet(Guid userId, Guid walletId);
    Task ProcessPendingSalaries();
}
