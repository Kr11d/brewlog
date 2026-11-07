using BrewLog.Domain.Common;
using BrewLog.Domain.Services;
using BrewLog.Domain.Users;
using BrewLog.Tests.Fakes;

namespace BrewLog.Tests.Services;

public sealed class AuthServiceTests
{
    private readonly FakeUserRepository _users = new();
    private readonly AuthService _auth;
    private readonly User _brewer;

    public AuthServiceTests()
    {
        _auth = new AuthService(_users, new FakePasswordHasher());
        _brewer = _users.Add(UserRole.Brewer, "brewer", "brewer123");
    }

    [Fact]
    public void Login_CorrectCredentials_ReturnsUserOfItsRole()
    {
        var user = _auth.Login("brewer", "brewer123");

        Assert.IsType<Brewer>(user);
        Assert.Equal(_brewer.Id, user.Id);
    }

    [Theory]
    [InlineData("nobody", "brewer123")]
    [InlineData("brewer", "wrong-password")]
    public void Login_UnknownLoginOrWrongPassword_ThrowsSameMessage(string login, string password)
    {
        var error = Assert.Throws<DomainException>(() => _auth.Login(login, password));

        Assert.Equal("Невірний логін або пароль", error.Message);
    }

    [Fact]
    public void Login_DisabledAccount_Throws()
    {
        _users.Add(UserRole.Assistant, "former", "former123", isActive: false);

        var error = Assert.Throws<DomainException>(() => _auth.Login("former", "former123"));

        Assert.Equal("Обліковий запис вимкнено", error.Message);
    }

    /// <summary>Вміст сховища — хеш, а не токен: навіть знаючи його, увійти не можна.</summary>
    [Fact]
    public void LoginWithToken_IssuedToken_ReturnsUserAndStoresOnlyHash()
    {
        var token = _auth.IssueRememberToken(_brewer);
        var stored = _users.GetById(_brewer.Id)!.RememberTokenHash;

        Assert.Equal(32, Convert.FromBase64String(token).Length);
        Assert.Equal(_brewer.Id, _auth.LoginWithToken(_brewer.Id, token)?.Id);
        Assert.NotNull(stored);
        Assert.NotEqual(token, stored);
        Assert.Null(_auth.LoginWithToken(_brewer.Id, stored));
    }

    [Fact]
    public void LoginWithToken_AfterRevoke_ReturnsNull()
    {
        var token = _auth.IssueRememberToken(_brewer);

        _auth.RevokeRememberToken(_brewer);

        Assert.Null(_auth.LoginWithToken(_brewer.Id, token));
        Assert.Null(_users.GetById(_brewer.Id)!.RememberTokenHash);
    }
}
