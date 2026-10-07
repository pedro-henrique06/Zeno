using System.Text.Json;

namespace Zeno.Application.Notifications;

/// <summary>Lê a resposta do servico de push do Expo. Cada ticket corresponde, na ordem, a um token do lote.</summary>
public static class ExpoTicketParser
{
    public static (int Success, List<string> Invalid) Parse(string json, IReadOnlyList<string> batch)
    {
        var invalid = new List<string>();
        var success = 0;

        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(json);
        }
        catch (JsonException)
        {
            return (0, invalid);
        }

        using (document)
        {
            if (!document.RootElement.TryGetProperty("data", out var data) || data.ValueKind != JsonValueKind.Array)
                return (0, invalid);

            var index = 0;
            foreach (var ticket in data.EnumerateArray())
            {
                if (index >= batch.Count)
                    break;

                var status = ticket.TryGetProperty("status", out var s) ? s.GetString() : null;
                if (status == "ok")
                {
                    success++;
                }
                else if (ticket.TryGetProperty("details", out var details) &&
                         details.TryGetProperty("error", out var error) &&
                         error.GetString() == "DeviceNotRegistered")
                {
                    // App desinstalado ou token rotacionado: o token deve ser desativado.
                    invalid.Add(batch[index]);
                }

                index++;
            }
        }

        return (success, invalid);
    }
}
