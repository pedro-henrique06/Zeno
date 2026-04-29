namespace Zeno.Application.Interfaces;

public interface IEncryptionService
{
    string Encrypt(string plainText);
    string Decrypt(string cipherText);
    string EncryptDecimal(decimal value);
    decimal DecryptDecimal(string cipherText);
}