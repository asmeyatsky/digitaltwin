using System;
using System.Security.Cryptography;
using System.Text;
using DigitalTwin.Core.Interfaces;
using DigitalTwin.Core.Telemetry;

namespace DigitalTwin.Core.Services
{
    public class EncryptionService : IEncryptionService
    {
        private readonly byte[] _key;

        public EncryptionService()
        {
            var keyBase64 = Environment.GetEnvironmentVariable("Encryption__Key");
            if (string.IsNullOrEmpty(keyBase64))
                throw new InvalidOperationException("Encryption__Key environment variable must be set (base64-encoded 256-bit key)");

            _key = Convert.FromBase64String(keyBase64);
            if (_key.Length != 32)
                throw new InvalidOperationException("Encryption__Key must be a 256-bit (32-byte) key");
        }

        public (byte[] ciphertext, byte[] iv, byte[] tag) Encrypt(string plaintext)
        {
            MetricsRegistry.EncryptionOperationsTotal.WithLabels("encrypt").Inc();
            var plaintextBytes = Encoding.UTF8.GetBytes(plaintext);
            var iv = new byte[12]; // 96-bit nonce for AES-GCM
            RandomNumberGenerator.Fill(iv);

            var ciphertext = new byte[plaintextBytes.Length];
            var tag = new byte[16]; // 128-bit authentication tag

            using var aes = new AesGcm(_key, tag.Length);
            aes.Encrypt(iv, plaintextBytes, ciphertext, tag);

            return (ciphertext, iv, tag);
        }

        public string Decrypt(byte[] ciphertext, byte[] iv, byte[] tag)
        {
            MetricsRegistry.EncryptionOperationsTotal.WithLabels("decrypt").Inc();
            var plaintext = new byte[ciphertext.Length];

            using var aes = new AesGcm(_key, tag.Length);
            aes.Decrypt(iv, ciphertext, tag, plaintext);

            return Encoding.UTF8.GetString(plaintext);
        }
    }
}
