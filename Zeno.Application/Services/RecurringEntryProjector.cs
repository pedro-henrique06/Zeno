using Zeno.Domain.Entry;
using Zeno.Domain.Enum;

namespace Zeno.Application.Services;

public static class RecurringEntryProjector
{
    public static IEnumerable<Entry> ExpandOccurrencesInRange(IEnumerable<Entry> recurringTemplates, DateTime rangeStart, DateTime rangeEnd)
    {
        foreach (var template in recurringTemplates)
        {
            var cursor = new DateTime(template.Date.Year, template.Date.Month, 1).AddMonths(1);

            while (cursor < rangeEnd)
            {
                var day = Math.Min(template.Date.Day, DateTime.DaysInMonth(cursor.Year, cursor.Month));
                var occurrenceDate = new DateTime(cursor.Year, cursor.Month, day);

                if (template.RecurrenceEndDate.HasValue && occurrenceDate > template.RecurrenceEndDate.Value)
                    break;

                if (occurrenceDate >= rangeStart && occurrenceDate < rangeEnd)
                {
                    yield return new Entry
                    {
                        Id = template.Id,
                        UserId = template.UserId,
                        Title = template.Title,
                        Value = template.Value,
                        Kind = template.Kind,
                        Description = template.Description,
                        TagId = template.TagId,
                        Date = occurrenceDate,
                        IsRecurring = true,
                        RecurrenceEndDate = template.RecurrenceEndDate
                    };
                }

                cursor = cursor.AddMonths(1);
            }
        }
    }

    public static decimal SumSignedBefore(IEnumerable<Entry> recurringTemplates, DateTime before)
    {
        decimal sum = 0;

        foreach (var template in recurringTemplates)
        {
            var cursor = new DateTime(template.Date.Year, template.Date.Month, 1).AddMonths(1);

            while (cursor < before)
            {
                if (template.RecurrenceEndDate.HasValue && cursor > template.RecurrenceEndDate.Value)
                    break;

                sum += template.Kind == EntryKind.Entrada ? template.Value : -template.Value;
                cursor = cursor.AddMonths(1);
            }
        }

        return sum;
    }
}
