namespace Zeno.Application.Interfaces;

/// <summary>Abstrai o relogio para que o agendamento do resumo diario seja testavel.</summary>
public interface IClock
{
    DateTime UtcNow { get; }
}

public sealed class SystemClock : IClock
{
    public DateTime UtcNow => DateTime.UtcNow;
}
