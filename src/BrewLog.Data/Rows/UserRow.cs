using BrewLog.Domain.Users;

namespace BrewLog.Data.Rows;

/// <summary>Рядок таблиці users. Об'єкт потрібного класу з нього створює UserFactory за колонкою role.</summary>
public sealed record UserRow
{
    public const string InsertSql = """
        INSERT INTO users (login, full_name, role, password_hash, is_active, remember_token_hash, created_at)
        VALUES (@Login, @FullName, @Role, @PasswordHash, @IsActive, @RememberTokenHash, @CreatedAt);
        """;

    public const string UpdateSql = """
        UPDATE users
        SET login = @Login, full_name = @FullName, role = @Role, password_hash = @PasswordHash,
            is_active = @IsActive, remember_token_hash = @RememberTokenHash
        WHERE id = @Id;
        """;

    public int Id { get; init; }

    public string Login { get; init; } = string.Empty;

    public string FullName { get; init; } = string.Empty;

    public string Role { get; init; } = string.Empty;

    public string PasswordHash { get; init; } = string.Empty;

    public int IsActive { get; init; }

    public string? RememberTokenHash { get; init; }

    public DateTime CreatedAt { get; init; }

    public static UserRow FromUser(User user) => new()
    {
        Id = user.Id,
        Login = user.Login,
        FullName = user.FullName,
        Role = user.Role.ToString(),
        PasswordHash = user.PasswordHash,
        IsActive = user.IsActive ? 1 : 0,
        RememberTokenHash = user.RememberTokenHash,
        CreatedAt = user.CreatedAt,
    };

    public User ToUser()
    {
        var user = UserFactory.Create(Enum.Parse<UserRole>(Role));
        user.Id = Id;
        user.Login = Login;
        user.FullName = FullName;
        user.PasswordHash = PasswordHash;
        user.IsActive = IsActive != 0;
        user.RememberTokenHash = RememberTokenHash;
        user.CreatedAt = CreatedAt;
        return user;
    }
}
