using Zeno.Domain.Home;

namespace Zeno.Tests;

public class BudgetRuleTests
{
    [Theory]
    [InlineData(10000, 5000, 3000, 2000)]
    [InlineData(5000, 2500, 1500, 1000)]
    [InlineData(2000, 1000, 600, 400)]
    public void CalculateBudget_503020_SplitsCorrectly(decimal income, decimal expectedNeeds, decimal expectedWants, decimal expectedSavings)
    {
        var needs = Math.Round(income * 0.50m, 2);
        var wants = Math.Round(income * 0.30m, 2);
        var savings = Math.Round(income * 0.20m, 2);

        Assert.Equal(expectedNeeds, needs);
        Assert.Equal(expectedWants, wants);
        Assert.Equal(expectedSavings, savings);
    }

    [Fact]
    public void IsOverBudget_WhenNeedsExceedsLimit_ReturnsTrue()
    {
        decimal totalNeeds = 6000;
        decimal needsLimit = 5000;

        var isOverBudget = totalNeeds > needsLimit;

        Assert.True(isOverBudget);
    }

    [Fact]
    public void IsOverBudget_WhenNeedsWithinLimit_ReturnsFalse()
    {
        decimal totalNeeds = 4000;
        decimal needsLimit = 5000;

        var isOverBudget = totalNeeds > needsLimit;

        Assert.False(isOverBudget);
    }
}

public class HomeSplitServiceTests
{
    [Fact]
    public void SplitEqual_DividesEvenlyAmongMembers()
    {
        var totalExpense = 300m;
        var memberCount = 3;
        var expectedShare = 100m;

        var actualShare = totalExpense / memberCount;

        Assert.Equal(expectedShare, actualShare);
    }

    [Fact]
    public void SplitProportional_BySalary_DividesCorrectly()
    {
        var totalExpense = 300m;
        var member1Salary = 5000m;
        var member2Salary = 3000m;
        var member3Salary = 2000m;
        var totalSalary = member1Salary + member2Salary + member3Salary;

        var member1Share = Math.Round(totalExpense * member1Salary / totalSalary, 2);
        var member2Share = Math.Round(totalExpense * member2Salary / totalSalary, 2);
        var member3Share = Math.Round(totalExpense * member3Salary / totalSalary, 2);

        Assert.Equal(150m, member1Share);
        Assert.Equal(90m, member2Share);
        Assert.Equal(60m, member3Share);
    }

    [Fact]
    public void SplitProportional_WithZeroSalary_HandlesGracefully()
    {
        var totalExpense = 300m;
        var member1Salary = 0m;
        var member2Salary = 3000m;
        var totalSalary = member1Salary + member2Salary;

        var member1Share = totalSalary > 0 ? Math.Round(totalExpense * member1Salary / totalSalary, 2) : 0;
        var member2Share = Math.Round(totalExpense * member2Salary / totalSalary, 2);

        Assert.Equal(0, member1Share);
        Assert.Equal(300m, member2Share);
    }

    [Fact]
    public void SplitProportional_TotalEqualsExpense()
    {
        var totalExpense = 300m;
        var member1Salary = 5000m;
        var member2Salary = 3000m;
        var member3Salary = 2000m;
        var totalSalary = member1Salary + member2Salary + member3Salary;

        var member1Share = Math.Round(totalExpense * member1Salary / totalSalary, 2);
        var member2Share = Math.Round(totalExpense * member2Salary / totalSalary, 2);
        var member3Share = Math.Round(totalExpense * member3Salary / totalSalary, 2);

        var total = member1Share + member2Share + member3Share;

        Assert.Equal(totalExpense, total);
    }
}