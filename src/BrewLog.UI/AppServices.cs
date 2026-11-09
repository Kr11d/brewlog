using BrewLog.Data.BeerXml;
using BrewLog.Data.Export;
using BrewLog.Data.Infrastructure;
using BrewLog.Data.Repositories;
using BrewLog.Data.Security;
using BrewLog.Data.Seed;
using BrewLog.Domain.Abstractions;
using BrewLog.Domain.Services;
using BrewLog.Domain.Users;
using BrewLog.UI.Features.Batches;
using BrewLog.UI.Features.Inventory;
using BrewLog.UI.Features.Measurements;
using BrewLog.UI.Features.Recipes;
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
        Session = new CurrentSession();
        RememberMe = new RememberMeStore(AppPaths.RememberMePath);

        var users = new UserRepository(Connections);
        var styles = new StyleRepository(Connections);
        var catalog = new IngredientCatalogRepository(Connections);
        var recipes = new RecipeRepository(Connections);
        Auth = new AuthService(users, PasswordHasher);
        Users = new UserService(users, PasswordHasher, Session, Clock);
        Catalog = new CatalogService(styles, catalog, Session);
        var batches = new BatchRepository(Connections);
        Inventory = new InventoryService(new StockRepository(Connections, Clock), catalog, recipes, batches, Session, Clock);
        Recipes = new RecipeService(recipes, catalog, new BeerXmlSerializer(styles), Session, Clock, Inventory);
        Batches = new BatchService(batches, recipes, styles, Recipes, Inventory, Session, Clock);

        Dialogs = new DialogService();
        Navigation = new NavigationService(CreatePageFactories());
        Windows = new WindowService(this);
        _databaseInitializer = new DatabaseInitializer(Connections, DemoSeeders.All(PasswordHasher));
    }

    public IClock Clock { get; }

    public IPasswordHasher PasswordHasher { get; }

    public SqliteConnectionFactory Connections { get; }

    /// <summary>Конкретний тип, а не ICurrentSession: вхід і вихід (SignIn, SignOut) виконує інтерфейс.</summary>
    public CurrentSession Session { get; }

    public RememberMeStore RememberMe { get; }

    public AuthService Auth { get; }

    public UserService Users { get; }

    public CatalogService Catalog { get; }

    public InventoryService Inventory { get; }

    public RecipeService Recipes { get; }

    public BatchService Batches { get; }

    public DialogService Dialogs { get; }

    public NavigationService Navigation { get; }

    public WindowService Windows { get; }

    /// <summary>Створює схему й демо-дані, якщо файлу БД ще немає.</summary>
    public void InitializeDatabase() => _databaseInitializer.Initialize(Clock.Now.Date);

    /// <summary>
    /// Автоматичний вхід за збереженим токеном «Запам'ятати мене». Недійсний токен стирається,
    /// щоб наступний запуск одразу показав вхід, не перевіряючи його знову.
    /// </summary>
    public bool TryRestoreSession()
    {
        var remembered = RememberMe.Load();
        if (remembered is null)
        {
            return false;
        }

        var user = Auth.LoginWithToken(remembered.UserId, remembered.Token);
        if (user is null)
        {
            RememberMe.Clear();
            return false;
        }

        Session.SignIn(user);
        return true;
    }

    /// <summary>
    /// Сторінки розділів. Сюди кожен розробник додає свою: [AppSection.Recipes] = () => new RecipesViewModel(...).
    /// Розділ без запису показує PlaceholderPage.
    /// </summary>
    private Dictionary<AppSection, Func<object>> CreatePageFactories() => new()
    {
        [AppSection.Recipes] = () => new RecipesViewModel(Recipes, Catalog, Session, Dialogs, Windows),
        [AppSection.Batches] = () => new BatchesViewModel(Batches, Users, Session, Clock, Dialogs, Windows, onlyMine: false),
        [AppSection.MyBatches] = () => new BatchesViewModel(Batches, Users, Session, Clock, Dialogs, Windows, onlyMine: true),
        [AppSection.Measurements] = () => new MeasurementsViewModel(Batches, Session, Clock, Dialogs, Windows),
        [AppSection.Inventory] = () => new InventoryViewModel(Inventory, Batches, Catalog, Session, Clock, Dialogs, Windows, new PurchaseOrderPdf(), new StockExcelExporter()),
    };
}
