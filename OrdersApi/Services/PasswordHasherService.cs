using System.Security.Cryptography;
using OrdersApi.Interfaces;

namespace OrdersApi.Services
{
    public class PasswordHasherService : IPasswordHasherService
    {
        private const string Algorithm  = "PBKDF2-SHA256";
        private const int    Iterations = 600_000;
        private const int    SaltSize   = 16;
        private const int    HashSize   = 32;

        public string Hash(string password)
        {
            var salt = RandomNumberGenerator.GetBytes(SaltSize);
            var hash = Rfc2898DeriveBytes.Pbkdf2(
                password, salt, Iterations, HashAlgorithmName.SHA256, HashSize);

            return $"{Algorithm}.{Iterations}.{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
        }

        public bool Verify(string? password, string? hashedPassword)
        {
            if (string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(hashedPassword))
                return false;

            var parts = hashedPassword.Split('.');
            if (parts.Length != 4) return false;

            if (parts[0] != Algorithm) return false;

            if (!int.TryParse(parts[1], out var iterCount) || iterCount <= 0)
                return false;

            byte[] salt, storedHash;
            try
            {
                salt       = Convert.FromBase64String(parts[2]);
                storedHash = Convert.FromBase64String(parts[3]);
            }
            catch (FormatException)
            {
                return false;
            }

            if (salt.Length == 0 || storedHash.Length == 0)
                return false;

            var computed = Rfc2898DeriveBytes.Pbkdf2(
                password, salt, iterCount, HashAlgorithmName.SHA256, storedHash.Length);

            return CryptographicOperations.FixedTimeEquals(computed, storedHash);
        }
    }
}
