using System;
using System.Security.Cryptography;

namespace Equinox.Infra.CrossCutting.Identity.Security
{
    public static class SecretKeyGenerator
    {
        public static string GenerateSecretKey(int length = 32)
        {
            var randomBytes = new byte[length];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomBytes);
            }
            return Convert.ToBase64String(randomBytes);
        }
    }
}
