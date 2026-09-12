namespace Zeno.Application.Requests;

public class YearQuery
{
    public int Year { get; set; } = DateTime.UtcNow.Year;
}
