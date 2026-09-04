using Identity.Service.Application.Interfaces;

namespace Identity.Service.Application.Services;

// Wrapper around BCrypt for password hashing (cost 12)
// Enterprise: Implements IPasswordHasher for DIP + testability (was static, now instance)
public class PasswordHasher : IPasswordHasher
{
    public string Hash(string password) => BCrypt.Net.BCrypt.HashPassword(password, 12);

    public bool Verify(string password, string hash) => BCrypt.Net.BCrypt.Verify(password, hash);
}
