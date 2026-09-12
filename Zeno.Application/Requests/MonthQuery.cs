namespace Zeno.Application.Requests;

public class MonthQuery
{
    public int Month { get; set; } = DateTime.UtcNow.Month;
    public int Year { get; set; } = DateTime.UtcNow.Year;
}
