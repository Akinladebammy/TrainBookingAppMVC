using System.Security.Cryptography;
using System.Text;

namespace TrainBookingAppMVC.PasswordValidation
{
    public class PasswordHashing : IPasswordHashing
    {
        public byte[] GenerateSalt()
        {
            return RandomNumberGenerator.GetBytes(16);
        }

        public string HashPassword(string password, byte[] salt)
        {
            using (var sha256 = SHA256.Create())
            {
                var encodedData = Encoding.UTF8.GetBytes(password + Convert.ToBase64String(salt));
                var hash = sha256.ComputeHash(encodedData);
                return Convert.ToHexString(hash);
            }
        }

        public bool VerifyPassword(string password, string storedHash, byte[] salt)
        {
            var hashedPassword = HashPassword(password, salt);
            return hashedPassword == storedHash;
        }
    }
}

