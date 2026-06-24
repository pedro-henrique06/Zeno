using System.Text.Json.Serialization;

namespace Zeno.Domain.Enum;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum Currency
{
    BRL = 0,
    USD = 1,
    EUR = 2
}
