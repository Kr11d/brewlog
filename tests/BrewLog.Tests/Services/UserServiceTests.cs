using BrewLog.Domain.Common;
using BrewLog.Domain.Services;
using BrewLog.Domain.Users;
using BrewLog.Tests.Fakes;

namespace BrewLog.Tests.Services;

public sealed class UserServiceTests
{
    private static readonly DateTime Now = new(2026, 3, 15, 10, 30, 0);

    private readonly FakeUserRepository _users = new();
    private readonly FakePasswordHasher _hasher = new();

    /// <summary>Дві зміни, після яких обліковий запис перестає бути активним власником.</summary>
    public static TheoryData<Action<UserService, int>> DisableOrDemote => new()
    {
        (service, id) => service.SetActive(id, false),
        (service, id) => service.Update(id, "Ірина Броварська", UserRole.Brewer),
    };

    /// <summary>Кожна з цих операцій над звичайним помічником вдалася б, якби не бракувало дозволу ManageUsers.</summary>
    public static TheoryData<Action<UserService, int>> ManageOperations => new()
    {
        (service, _) => service.GetAll(),
        (service, _) => service.Create("newuser", "Нова Людина", UserRole.Assistant, "secret1"),
        (service, id) => service.Update(id, "Інше Ім'я", UserRole.Assistant),
        (service, id) => service.ResetPassword(id, "secret2"),
        (service, id) => service.SetActive(id, false),
    };

    [Fact]
    public void Create_ValidData_StoresActiveUserOfRoleWithPasswordHash()
    {
        var service = ServiceFor(_users.Add(UserRole.Owner, "owner"));

        var created = service.Create(" marta2 ", "Марта Хмелевська", UserRole.Assistant, "secret1");

        var stored = _users.FindByLogin("marta2");
        Assert.NotNull(stored);
        Assert.Equal(created.Id, stored.Id);
        Assert.IsType<Assistant>(stored);
        Assert.True(stored.IsActive);
        Assert.NotEqual("secret1", stored.PasswordHash);
        Assert.True(_hasher.Verify("secret1", stored.PasswordHash));
        Assert.Equal(Now, stored.CreatedAt);
    }

    [Theory]
    [InlineData("ab", "secret1")]
    [InlineData("abcdefghijabcdefghijabcdefghijabc", "secret1")]
    [InlineData("марта", "secret1")]
    [InlineData("new user", "secret1")]
    [InlineData("newuser", "12345")]
    [InlineData("BREWER", "secret1")]
    public void Create_InvalidLoginPasswordOrDuplicate_ThrowsAndStoresNothing(string login, string password)
    {
        var service = ServiceFor(_users.Add(UserRole.Owner, "owner"));
        _users.Add(UserRole.Brewer, "brewer");

        Assert.Throws<DomainException>(() => service.Create(login, "Нова Людина", UserRole.Assistant, password));

        Assert.Equal(2, _users.GetAll().Count);
    }

    /// <summary>Другий активний власник є, тож заборону дає саме правило «не себе».</summary>
    [Theory]
    [MemberData(nameof(DisableOrDemote))]
    public void ChangeOwnAccount_DisableOrDemote_ThrowsAndKeepsAccount(Action<UserService, int> change)
    {
        var owner = _users.Add(UserRole.Owner, "owner");
        _users.Add(UserRole.Owner, "partner");

        Assert.Throws<DomainException>(() => change(ServiceFor(owner), owner.Id));

        AssertActiveOwner(owner.Id);
    }

    /// <summary>Дію виконує сесія без облікового запису в сховищі: правило «не себе» не втручається.</summary>
    [Theory]
    [MemberData(nameof(DisableOrDemote))]
    public void ChangeLastActiveOwner_DisableOrDemote_ThrowsAndKeepsOwner(Action<UserService, int> change)
    {
        var lastOwner = _users.Add(UserRole.Owner, "owner");
        _users.Add(UserRole.Owner, "former", isActive: false);
        var service = new UserService(_users, _hasher, new FakeSession(null, Permission.ManageUsers), new FixedClock(Now));

        var error = Assert.Throws<DomainException>(() => change(service, lastOwner.Id));

        Assert.Equal("Має лишитися хоча б один активний власник.", error.Message);
        AssertActiveOwner(lastOwner.Id);
    }

    [Theory]
    [MemberData(nameof(ManageOperations))]
    public void ManageOperation_NoManageUsers_Throws(Action<UserService, int> operation)
    {
        _users.Add(UserRole.Owner, "owner");
        var brewer = _users.Add(UserRole.Brewer, "brewer");
        var assistant = _users.Add(UserRole.Assistant, "marta");

        Assert.Throws<DomainException>(() => operation(ServiceFor(brewer), assistant.Id));
    }

    [Fact]
    public void GetAssistants_BrewerSession_ReturnsOnlyActiveAssistants()
    {
        var brewer = _users.Add(UserRole.Brewer, "brewer");
        _users.Add(UserRole.Owner, "owner");
        var active = _users.Add(UserRole.Assistant, "marta");
        _users.Add(UserRole.Assistant, "former", isActive: false);

        var assistants = ServiceFor(brewer).GetAssistants();

        Assert.Equal(active.Id, Assert.Single(assistants).Id);
    }

    /// <summary>Сервіс від імені користувача з дозволами саме його ролі.</summary>
    private UserService ServiceFor(User actor) =>
        new(_users, _hasher, new FakeSession(actor, [.. actor.Permissions]), new FixedClock(Now));

    private void AssertActiveOwner(int id)
    {
        var stored = _users.GetById(id);
        Assert.IsType<Owner>(stored);
        Assert.True(stored.IsActive);
    }
}
