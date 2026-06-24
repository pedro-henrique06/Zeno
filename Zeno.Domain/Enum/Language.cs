using System.Text.Json.Serialization;

namespace Zeno.Domain.Enum;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum Language
{
    PtBR = 0,
    EnUS = 1,
    Es = 2
}
