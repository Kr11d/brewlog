using BrewLog.Data.Repositories;
using BrewLog.Data.Seed;
using BrewLog.Domain.Users;
using BrewLog.Tests.Fakes;

namespace BrewLog.Tests.Data;

public sealed class UserRepositoryTests : IDisposable
{
    private readonly FakePasswordHasher _hasher = new();
    private readonly TempDatabase _database;
    private readonly UserRepository _users;

    public UserRepositoryTests()
    {
        _database = new TempDatabase([new UserSeeder(_hasher)]);
        _users = new UserRepository(_database.Connections);
    }

    public void Dispose() => _database.Dispose();

    [Theory]
    [InlineData("BREWER", "brewer123", typeof(Brewer))]
    [InlineData("Assistant", "assistant123", typeof(Assistant))]
    [InlineData("owner", "owner123", typeof(Owner))]
    public void FindByLogin_DemoUserAnyCase_ReturnsRoleClassWithPasswordHashOnly(string login, string password, Type expectedType)
    {
        var user = _users.FindByLogin(login);

        Assert.NotNull(user);
        Assert.IsType(expectedType, user);
        Assert.True(user.IsActive);
        Assert.NotEqual(password, user.PasswordHash);
        Assert.True(_hasher.Verify(password, user.PasswordHash));
    }

    [Fact]
    public void UpdateThenGetById_ChangedRoleAndToken_RestoresNewClassAndFields()
    {
        var assistant = _users.FindByLogin("assistant")!;
        var promoted = assistant.WithRole(UserRole.Brewer);
        promoted.FullName = "Марта Хмелевська-Солодовник";
        promoted.IsActive = false;

        _users.Update(promoted);
        _users.UpdateRememberToken(assistant.Id, "token-hash");

        var stored = _users.GetById(assistant.Id);
        Assert.IsType<Brewer>(stored);
        Assert.Equal("assistant", stored.Login);
        Assert.Equal("Марта Хмелевська-Солодовник", stored.FullName);
        Assert.False(stored.IsActive);
        Assert.Equal("token-hash", stored.RememberTokenHash);
        Assert.Equal(_database.Today.AddMonths(-9), stored.CreatedAt);
    }
}
