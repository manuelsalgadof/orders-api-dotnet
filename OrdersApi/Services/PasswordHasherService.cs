using System.Security.Cryptography;
using OrdersApi.Interfaces;

namespace OrdersApi.Services
{
    public class PasswordHasherService : IPasswordHasherService
    {
        private const int Iterations = 100_000;
        private const int SaltSize  = 16;
        private const int HashSize  = 32;

        public string Hash(string password)
        {
            var salt = RandomNumberGenerator.GetBytes(SaltSize);
            var hash = Rfc2898DeriveBytes.Pbkdf2(
                password, salt, Iterations, HashAlgorithmName.SHA256, HashSize);

            return $"{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
        }

        public bool Verify(string password, string hashedPassword)
        {
            var parts = hashedPassword.Split('.');
            if (parts.Length != 2) return false;

            byte[] salt, storedHash;
            try
            {
                salt       = Convert.FromBase64String(parts[0]);
                storedHash = Convert.FromBase64String(parts[1]);
            }
            catch
            {
                return false;
            }

            var computed = Rfc2898DeriveBytes.Pbkdf2(
                password, salt, Iterations, HashAlgorithmName.SHA256, HashSize);

            return CryptographicOperations.FixedTimeEquals(computed, storedHash);
        }
    }
}
