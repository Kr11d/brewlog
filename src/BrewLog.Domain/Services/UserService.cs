using BrewLog.Domain.Abstractions;
using BrewLog.Domain.Common;
using BrewLog.Domain.Users;

namespace BrewLog.Domain.Services;

/// <summary>
/// Керування обліковими записами (дозвіл ManageUsers): створення, зміна імені й ролі, скидання пароля, вимкнення.
/// Стежить, щоб користувач не заблокував сам себе і щоб лишався хоча б один активний власник.
/// </summary>
public sealed class UserService
{
    private const int MinPasswordLength = 6;

    private readonly IUserRepository _users;
    private readonly IPasswordHasher _hasher;
    private readonly ICurrentSession _session;
    private readonly IClock _clock;

    public UserService(IUserRepository users, IPasswordHasher hasher, ICurrentSession session, IClock clock)
    {
        _users = users;
        _hasher = hasher;
        _session = session;
        _clock = clock;
    }

    public IReadOnlyList<User> GetAll()
    {
        _session.Require(Permission.ManageUsers);
        return _users.GetAll();
    }

    /// <summary>
    /// Активні помічники — кандидати у виконавці партії. Потрібні пивоварові (ManageBatches),
    /// який не має доступу до повного списку користувачів.
    /// </summary>
    public IReadOnlyList<User> GetAssistants()
    {
        _session.Require(Permission.ManageBatches);
        return _users.GetAll().OfType<Assistant>().Where(user => user.IsActive).ToList<User>();
    }

    /// <summary>Перевіряє дані й створює активний обліковий запис; пароль зберігається лише як хеш.</summary>
    public User Create(string login, string fullName, UserRole role, string password)
    {
        _session.Require(Permission.ManageUsers);

        var user = UserFactory.Create(role);
        user.Login = login.Trim();
        user.FullName = fullName.Trim();

        var errors = new List<string>(user.Validate());
        errors.AddRange(ValidatePassword(password));
        DomainException.ThrowIfAny(errors);

        if (_users.FindByLogin(user.Login) is not null)
        {
            throw new DomainException($"Логін «{user.Login}» уже зайнятий.");
        }

        user.PasswordHash = _hasher.Hash(password);
        user.CreatedAt = _clock.Now;
        _users.Insert(user);
        return user;
    }

    /// <summary>Змінює ім'я та роль. Власну роль змінити не можна, а останній активний власник лишається власником.</summary>
    public void Update(int id, string fullName, UserRole role)
    {
        _session.Require(Permission.ManageUsers);

        var current = GetExisting(id);
        if (IsCurrentUser(id) && role != current.Role)
        {
            throw new DomainException("Не можна змінити власну роль.");
        }

        var updated = current.WithRole(role);
        updated.FullName = fullName.Trim();
        DomainException.ThrowIfAny(updated.Validate());
        EnsureActiveOwnerRemains(updated);

        _users.Update(updated);
    }

    /// <summary>Задає новий пароль і відкликає токен «Запам'ятати мене»: старий автоматичний вхід більше не діє.</summary>
    public void ResetPassword(int id, string newPassword)
    {
        _session.Require(Permission.ManageUsers);

        var user = GetExisting(id);
        DomainException.ThrowIfAny(ValidatePassword(newPassword));

        user.PasswordHash = _hasher.Hash(newPassword);
        user.RememberTokenHash = null;
        _users.Update(user);
    }

    /// <summary>Вмикає або вимикає обліковий запис. Себе вимкнути не можна; вимкнений запис втрачає токен «Запам'ятати мене».</summary>
    public void SetActive(int id, bool isActive)
    {
        _session.Require(Permission.ManageUsers);

        if (!isActive && IsCurrentUser(id))
        {
            throw new DomainException("Не можна вимкнути власний обліковий запис.");
        }

        var updated = GetExisting(id).Clone();
        updated.IsActive = isActive;
        if (!isActive)
        {
            updated.RememberTokenHash = null;
        }

        EnsureActiveOwnerRemains(updated);
        _users.Update(updated);
    }

    private static IReadOnlyList<string> ValidatePassword(string password) =>
        password.Length < MinPasswordLength
            ? [$"Пароль має містити щонайменше {MinPasswordLength} символів."]
            : [];

    private User GetExisting(int id) => _users.GetById(id) ?? throw new DomainException("Користувача не знайдено.");

    private bool IsCurrentUser(int id) => _session.User?.Id == id;

    /// <summary>
    /// Перевіряє стан «після зміни»: хтось активний має й надалі керувати користувачами (а це власник).
    /// Рахуємо через дозвіл, а не через тип, тож правило не залежить від того, яка роль його має.
    /// </summary>
    private void EnsureActiveOwnerRemains(User changed)
    {
        var hasActiveOwner = _users.GetAll()
            .Select(user => user.Id == changed.Id ? changed : user)
            .Any(user => user.IsActive && user.Can(Permission.ManageUsers));

        if (!hasActiveOwner)
        {
            throw new DomainException("Має лишитися хоча б один активний власник.");
        }
    }
}
