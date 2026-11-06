namespace BrewLog.Domain.Abstractions;

/// <summary>Хешування паролів і токенів. Domain не залежить від конкретного алгоритму.</summary>
public interface IPasswordHasher
{
    string Hash(string password);

    bool Verify(string password, string hash);
}
