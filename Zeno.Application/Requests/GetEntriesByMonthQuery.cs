namespace Zeno.Application.Requests;

public class GetEntriesByMonthQuery
{
    public int? Month { get; set; }
    public int? Year { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}
