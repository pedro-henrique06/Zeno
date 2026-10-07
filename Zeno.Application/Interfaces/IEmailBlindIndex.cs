namespace Zeno.Application.Interfaces;

/// <summary>
/// Índice cego do e-mail: um HMAC determinístico que permite achar o usuário pelo e-mail sem guardar o
/// e-mail em texto puro. Não dá para reverter o hash para o e-mail sem a chave.
/// </summary>
public interface IEmailBlindIndex
{
    string Compute(string email);
}
