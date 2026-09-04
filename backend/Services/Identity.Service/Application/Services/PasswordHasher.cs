namespace Identity.Service.Application.Services;

// Wrapper around BCrypt for password hashing (cost 12)
public static class PasswordHasher
{
    public static string Hash(string password) => BCrypt.Net.BCrypt.HashPassword(password, 12);

    public static bool Verify(string password, string hash) => BCrypt.Net.BCrypt.Verify(password, hash);
}
