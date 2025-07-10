namespace TrainBookingAppMVC.PasswordValidation
{
    public interface IPasswordHashing
    {
        byte[] GenerateSalt();
        string HashPassword(string password, byte[] salt);
        bool VerifyPassword(string password, string storedHash, byte[] salt);
    }
}
