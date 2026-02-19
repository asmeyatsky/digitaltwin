namespace DigitalTwin.Core.Interfaces
{
    public interface IEncryptionService
    {
        (byte[] ciphertext, byte[] iv, byte[] tag) Encrypt(string plaintext);
        string Decrypt(byte[] ciphertext, byte[] iv, byte[] tag);
    }
}
