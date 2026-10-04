using System.Globalization;
using System.Text;

namespace Zeno.Application.Services;

/// <summary>Converte textos de valor monetário vindos de automações ("R$ 1.234,56") em decimal.</summary>
public static class AmountParser
{
    public static bool TryParse(string? text, out decimal value)
    {
        value = 0m;
        if (string.IsNullOrWhiteSpace(text))
            return false;

        var negative = text.Contains('-') || text.Contains('−') || (text.Contains('(') && text.Contains(')'));

        var sb = new StringBuilder();
        foreach (var c in text)
        {
            if (char.IsDigit(c) || c == ',' || c == '.')
                sb.Append(c);
        }

        var cleaned = sb.ToString().Trim('.', ',');
        if (cleaned.Length == 0)
            return false;

        var lastComma = cleaned.LastIndexOf(',');
        var lastDot = cleaned.LastIndexOf('.');

        string normalized;
        if (lastComma >= 0 && lastDot >= 0)
        {
            // O último separador é o decimal; o outro é de milhar.
            var decimalSep = lastComma > lastDot ? ',' : '.';
            var thousandSep = decimalSep == ',' ? '.' : ',';
            normalized = cleaned.Replace(thousandSep.ToString(), "").Replace(decimalSep, '.');
        }
        else if (lastComma >= 0)
        {
            normalized = IsThousandsOnly(cleaned, ',') ? cleaned.Replace(",", "") : cleaned.Replace(',', '.');
        }
        else if (lastDot >= 0)
        {
            normalized = IsThousandsOnly(cleaned, '.') ? cleaned.Replace(".", "") : cleaned;
        }
        else
        {
            normalized = cleaned;
        }

        if (!decimal.TryParse(normalized, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var parsed))
            return false;

        value = negative ? -parsed : parsed;
        return true;
    }

    /// <summary>"1.000" ou "1.234.567" (grupos de 3 dígitos após o separador) é milhar, não decimal.</summary>
    private static bool IsThousandsOnly(string text, char separator)
    {
        var parts = text.Split(separator);
        if (parts.Length < 2 || parts[0].Length is < 1 or > 3)
            return false;

        return parts.Skip(1).All(p => p.Length == 3);
    }
}
