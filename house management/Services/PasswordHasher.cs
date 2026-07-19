using System;
using System.Security.Cryptography;
using System.Text;

namespace house_management.Services
{
    /// <summary>
    /// Secure password hashing using PBKDF2-SHA256 with a per-user random salt.
    /// Output format: iterations.saltBase64.hashBase64
    /// </summary>
    public static class PasswordHasher
    {
        private const int SaltBytes = 16;
        private const int HashBytes = 32;
        private const int Iterations = 10000;

        private static readonly HashAlgorithmName Algorithm = HashAlgorithmName.SHA256;

        public static string Hash(string password)
        {
            if (string.IsNullOrEmpty(password))
                throw new ArgumentException("Password cannot be null or empty.", nameof(password));

            byte[] salt = new byte[SaltBytes];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(salt);
            }

            byte[] hash = ComputeHash(password, salt, Iterations);

            return $"{Iterations}.{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
        }

        public static bool Verify(string password, string storedHash)
        {
            if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(storedHash))
                return false;

            string[] parts = storedHash.Split('.');
            if (parts.Length != 3)
                return false;

            if (!int.TryParse(parts[0], out int iterations))
                return false;

            byte[] salt;
            byte[] expectedHash;
            try
            {
                salt = Convert.FromBase64String(parts[1]);
                expectedHash = Convert.FromBase64String(parts[2]);
            }
            catch (FormatException)
            {
                return false;
            }

            byte[] actualHash = ComputeHash(password, salt, iterations);

            return ConstantTimeEquals(actualHash, expectedHash);
        }

        /// <summary>
        /// Detects legacy plaintext passwords (those without the PBKDF2 dotted format)
        /// so they can be migrated transparently on the next successful login.
        /// </summary>
        public static bool NeedsMigration(string storedHash)
        {
            if (string.IsNullOrEmpty(storedHash))
                return true;

            string[] parts = storedHash.Split('.');
            if (parts.Length != 3)
                return true;

            return !int.TryParse(parts[0], out _);
        }

        private static byte[] ComputeHash(string password, byte[] salt, int iterations)
        {
            using (var derive = new Rfc2898DeriveBytes(password, salt, iterations, Algorithm))
            {
                return derive.GetBytes(HashBytes);
            }
        }

        private static bool ConstantTimeEquals(byte[] a, byte[] b)
        {
            if (a == null || b == null || a.Length != b.Length)
                return false;

            int diff = 0;
            for (int i = 0; i < a.Length; i++)
            {
                diff |= a[i] ^ b[i];
            }
            return diff == 0;
        }
    }
}
