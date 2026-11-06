using BrewLog.Data.Infrastructure;
using BrewLog.Data.Security;
using BrewLog.Data.Seed;
using BrewLog.Domain.Abstractions;
using BrewLog.Domain.Users;
using BrewLog.UI.Services;

namespace BrewLog.UI;

/// <summary>Композиція залежностей без DI-контейнера: тут створюються й пов'язуються всі сервіси застосунку.</summary>
public sealed class AppServices
{
    private readonly DatabaseInitializer _databaseInitializer;

    /// <param name="databasePath">Шлях до файлу БД: робочий або тимчасовий для smoke-перевірки.</param>
    /// <param name="pooling">false для тимчасової БД, щоб її файл можна було видалити.</param>
    public AppServices(string databasePath, bool pooling = true)
    {
        Clock = new SystemClock();
        PasswordHasher = new BCryptPasswordHasher();
        Connections = new SqliteConnectionFactory(databasePath, pooling);
        Session = new AllowAllSession();
        Dialogs = new DialogService();
        Navigation = new NavigationService(CreatePageFactories());
        Windows = new WindowService(this);
        _databaseInitializer = new DatabaseInitializer(Connections, DemoSeeders.All(PasswordHasher));
    }

    public IClock Clock { get; }

    public IPasswordHasher PasswordHasher { get; }

    public SqliteConnectionFactory Connections { get; }

    public ICurrentSession Session { get; }

    public DialogService Dialogs { get; }

    public NavigationService Navigation { get; }

    public WindowService Windows { get; }

    /// <summary>Створює схему й демо-дані, якщо файлу БД ще немає.</summary>
    public void InitializeDatabase() => _databaseInitializer.Initialize(Clock.Now.Date);

    /// <summary>
    /// Сторінки розділів. Сюди кожен розробник додає свою: [AppSection.Recipes] = () => new RecipesViewModel(...).
    /// Розділ без запису показує PlaceholderPage.
    /// </summary>
    private Dictionary<AppSection, Func<object>> CreatePageFactories() => new();
}
