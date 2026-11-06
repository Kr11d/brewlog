using BrewLog.Domain.Abstractions;

namespace BrewLog.Data.Security;

/// <summary>Хешування паролів і токенів алгоритмом BCrypt.</summary>
public sealed class BCryptPasswordHasher : IPasswordHasher
{
    public string Hash(string password) => BCrypt.Net.BCrypt.HashPassword(password);

    public bool Verify(string password, string hash) => BCrypt.Net.BCrypt.Verify(password, hash);
}
