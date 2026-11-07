using System.IO;
using System.Text.Json;

namespace BrewLog.UI.Services;

/// <summary>
/// Зберігає вхід «Запам'ятати мене» у файлі {userId, token}. Сам токен нічого не дає без перевірки AuthService,
/// а пошкоджений чи порожній файл вважається відсутнім: тоді користувач просто входить заново.
/// </summary>
public sealed class RememberMeStore
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly string _path;

    /// <param name="path">Шлях до файлу; типово — AppPaths.RememberMePath.</param>
    public RememberMeStore(string path)
    {
        _path = path;
    }

    public void Save(int userId, string token)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(_path))!);
        File.WriteAllText(_path, JsonSerializer.Serialize(new RememberedLogin(userId, token), JsonOptions));
    }

    /// <summary>Повертає збережений вхід або null, якщо файлу немає чи його не вдалося прочитати.</summary>
    public RememberedLogin? Load()
    {
        try
        {
            var login = File.Exists(_path)
                ? JsonSerializer.Deserialize<RememberedLogin>(File.ReadAllText(_path), JsonOptions)
                : null;

            return login is { UserId: > 0, Token.Length: > 0 } ? login : null;
        }
        catch (Exception exception) when (exception is JsonException or IOException or UnauthorizedAccessException)
        {
            return null;
        }
    }

    public void Clear()
    {
        if (File.Exists(_path))
        {
            File.Delete(_path);
        }
    }
}
