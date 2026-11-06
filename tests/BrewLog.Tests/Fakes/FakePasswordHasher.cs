using BrewLog.Domain.Abstractions;

namespace BrewLog.Tests.Fakes;

/// <summary>Миттєвий «хеш» для тестів: справжній BCrypt навмисно повільний.</summary>
public sealed class FakePasswordHasher : IPasswordHasher
{
    public string Hash(string password) => $"hash:{password}";

    public bool Verify(string password, string hash) => hash == Hash(password);
}
