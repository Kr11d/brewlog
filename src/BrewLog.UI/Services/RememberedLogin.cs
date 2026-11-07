namespace BrewLog.UI.Services;

/// <summary>Вміст файлу «Запам'ятати мене»: кого впізнати й чим це підтвердити. У БД зберігається лише хеш токена.</summary>
public sealed record RememberedLogin(int UserId, string Token);
