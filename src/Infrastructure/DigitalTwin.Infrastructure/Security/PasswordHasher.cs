using System;
using System.Security.Cryptography;
using System.Text;

namespace DigitalTwin.Infrastructure.Security
{
    /// <summary>
    /// Secure Password Hashing Service
    /// 
    /// Implements PBKDF2 with proper salt and iteration count
    /// for secure password storage and verification.
    /// </summary>
    public static class PasswordHasher
    {
        private const int SaltSize = 16; // 128 bit
        private const int KeySize = 32; // 256 bit
        private const int Iterations = 100000; // OWASP recommended minimum

        /// <summary>
        /// Hashes a password with a random salt
        /// </summary>
        public static string HashPassword(string password)
        {
            if (string.IsNullOrEmpty(password))
                throw new ArgumentException("Password cannot be null or empty", nameof(password));

            using (var algorithm = new Rfc2898DeriveBytes(
                password,
                SaltSize,
                Iterations,
                HashAlgorithmName.SHA256))
            {
                var key = Convert.ToBase64String(algorithm.GetBytes(KeySize));
                var salt = Convert.ToBase64String(algorithm.Salt);
                
                return $"{Iterations}.{salt}.{key}";
            }
        }

        /// <summary>
        /// Verifies a password against its hash
        /// </summary>
        public static bool VerifyPassword(string password, string hash)
        {
            if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(hash))
                return false;

            try
            {
                var parts = hash.Split('.', 3);
                if (parts.Length != 3)
                    return false;

                var iterations = Convert.ToInt32(parts[0]);
                var salt = Convert.FromBase64String(parts[1]);
                var key = Convert.FromBase64String(parts[2]);

                using (var algorithm = new Rfc2898DeriveBytes(
                    password,
                    salt,
                    iterations,
                    HashAlgorithmName.SHA256))
                {
                    var keyToCheck = algorithm.GetBytes(KeySize);
                    var verified = keyToCheck.Length == key.Length;
                    
                    for (int i = 0; i < keyToCheck.Length && i < key.Length; i++)
                    {
                        verified &= keyToCheck[i] == key[i];
                    }
                    
                    return verified;
                }
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Checks if a password needs rehashing (for future algorithm updates)
        /// </summary>
        public static bool NeedsRehash(string hash)
        {
            try
            {
                var parts = hash.Split('.', 3);
                if (parts.Length != 3)
                    return true;

                var iterations = Convert.ToInt32(parts[0]);
                return iterations < Iterations;
            }
            catch
            {
                return true;
            }
        }

        /// <summary>
        /// Generates a cryptographically secure random password
        /// </summary>
        public static string GenerateSecurePassword(int length = 16)
        {
            const string validChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789!@#$%^&*()_+-=[]{}|;:,.<>?";
            
            using (var rng = RandomNumberGenerator.Create())
            {
                var result = new StringBuilder();
                var buffer = new byte[length];
                
                rng.GetBytes(buffer);
                
                for (int i = 0; i < length; i++)
                {
                    result.Append(validChars[buffer[i] % validChars.Length]);
                }
                
                return result.ToString();
            }
        }
    }
}