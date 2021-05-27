using System.Security.Cryptography;
using System;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;

namespace Bookstore.Utilities
{
    public class Crypto
    {
        // Derived from: https://docs.microsoft.com/en-us/aspnet/core/security/data-protection/consumer-apis/password-hashing?view=aspnetcore-5.0
        private static byte[] GenerateSalt()
        {
           // generate a 128-bit salt using a secure PRNG
           byte[] salt = new byte[128 / 8];
           using var rng = RandomNumberGenerator.Create();
           rng.GetBytes(salt);
           return salt;
        }

        private static byte[] GeneratePasswordHash(string password, byte[] salt)
        {
            return KeyDerivation.Pbkdf2(password, salt, KeyDerivationPrf.HMACSHA1, 10000, 256 / 8);
        }

        // Returns: (passwordHash, salt) as base64
        public static (string, string) GeneratePasswordHash(string password)
        {
            byte[] salt = GenerateSalt();
            byte[] passwordHash = GeneratePasswordHash(password, salt);
            return (
                Convert.ToBase64String(passwordHash),
                Convert.ToBase64String(salt)
            );
        }

        public static bool PasswordHashMatches(string maybePassword, string passwordHashBase64, string saltBase64)
        {
            string maybePasswordHashBase64 = Convert.ToBase64String(GeneratePasswordHash(maybePassword, Convert.FromBase64String(saltBase64)));
            return maybePasswordHashBase64 == passwordHashBase64;
        }
    }
}