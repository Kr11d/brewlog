# BrewLog

Настільний застосунок для обліку виробництва малої броварні: рецепти з живим розрахунком показників, партії з етапами, журнал бродіння, склад сировини, розлив із PDF-етикетками, звіти й обмін рецептами у форматі BeerXML.

Командний навчальний проєкт із дисципліни «Парадигма ООП» (лабораторні роботи № 1–3). Стек: C#, .NET 10, WPF, SQLite. Статус: готовий до здачі.

## Запуск

Потрібні Windows і [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0).

```bash
dotnet run --project src/BrewLog.UI
```

Під час першого запуску застосунок створює базу даних `%LOCALAPPDATA%\BrewLog\brewlog.db`.

Перевірка без показу вікон (код виходу 0 — успіх, лог у `%TEMP%\brewlog-smoke.log`) і тести:

```bash
dotnet run --project src/BrewLog.UI -- --smoke
```

```bash
dotnet test
```

### Демо-облікові записи

| Логін | Пароль | Роль |
|---|---|---|
| `brewer` | `brewer123` | Пивовар |
| `assistant` | `assistant123` | Помічник |
| `owner` | `owner123` | Власник |

### Як скинути демо-дані

Закрийте застосунок і видаліть файл `%LOCALAPPDATA%\BrewLog\brewlog.db`. Під час наступного запуску база створиться заново, а демо-дати рахуватимуться від поточного дня.

### Скриншоти

Знімки екранів лежать у папці `docs/screenshots/`.

## Архітектура

```text
BrewLog.UI      WPF, MVVM (CommunityToolkit.Mvvm): вікна, сторінки, ViewModel, тема, навігація
    │           вікна не містять розрахунків і SQL; композиція залежностей — AppServices
    ▼
BrewLog.Data    SQLite (Dapper), BeerXML, PDF (QuestPDF), Excel (ClosedXML), BCrypt
    │           реалізує інтерфейси сховищ, оголошені в Domain
    ▼
BrewLog.Domain  сутності, ієрархії, стани, стратегії, фабрики, сервіси; без NuGet-залежностей
```

Сервіси перевіряють дозволи й кидають `DomainException` з українським повідомленням, а ViewModel показує його через `DialogService`. Окремі вікна відкриває лише `WindowService`. Smoke-перевірка на тимчасовій БД створює кожне вікно й сторінку за кожну роль.

## Бібліотеки

| Бібліотека | Проєкт | Для чого |
|---|---|---|
| CommunityToolkit.Mvvm | UI | MVVM: `ObservableObject`, `[ObservableProperty]`, `[RelayCommand]` |
| ScottPlot.WPF | UI | графіки бродіння й обсягу, зображення графіка для PDF-звіту |
| QuestPDF | Data | етикетки, замовлення постачальнику, звіт у PDF |
| ClosedXML | Data | експорт складу й звітів в Excel |
| Dapper, Microsoft.Data.Sqlite | Data | база даних |
| BCrypt.Net-Next | Data | хешування паролів і токена «Запам'ятати мене» |
| xunit, xunit.runner.visualstudio, Microsoft.NET.Test.Sdk, coverlet.collector | Tests | тести й покриття |

## Рецепти й розрахунки

Кожен рядок рецепта — об'єкт свого класу, який сам додає внесок через `Ingredient.ApplyTo(RecipeStats)`: `RecipeCalculator` лише викликає цей метод для кожного рядка, тож перевірок типу в розрахунках немає. Солод дає екстракт і колір, додаток — екстракт без ефективності, хміль реєструє варильне внесення (сухе охмелення гіркоти не додає), дріжджі задають зброджування й температури. Формулу IBU обирає рецепт: він зберігає її код, а `IbuFormulaFactory` створює потрібну стратегію.

| Показник | Формула |
|---|---|
| OG | `1 + пункти / 1000`, де `пункти = Σ кг · Yield/100 · 385,6 / обсяг_л`; для зерна ще `· ефективність/100` |
| FG | `1 + (OG − 1) · (1 − A)`, де `A` — максимальне зброджування серед дріжджів, без дріжджів 0,75 |
| ABV, % | `(OG − FG) · 131,25` |
| IBU Tinseth | `1,65 · 0,000125^(G − 1) · (1 − e^(−0,04·t)) / 4,15 · (α · грами · 1000 / л)` |
| IBU Rager | `грами · U · α · 1000 / (л · (1 + GA))`, де `U = (18,11 + 13,86 · tanh((t − 31,32) / 18,27)) / 100`, а `GA = (G − 1,050) / 0,2` при `G > 1,050`, інакше 0 |
| SRM (Morey) | `1,4922 · MCU^0,6859`, де `MCU = Σ (кг · 2,20462 · °L) / (л · 0,264172)`; `EBC = SRM · 1,97` |
| Собівартість, грн/л | `Σ кількість · ціна одиниці / обсяг` |

`G` — OG рецепта, `α` — частка (0,10), `t` — час кипіння, хв. Ціна одиниці — середньозважена по залишках складу, а без залишків — остання ціна з довідника.

## Користувачі й ролі

Роль — це клас (`Brewer`, `Assistant`, `Owner`), який сам визначає дозволи (`Permissions`), пункти меню (`Sections`) і домашній розділ (`HomeSection`). Тому код питає `user.Can(permission)`, а не порівнює ролі. Екрани ховають кнопки без дозволу, а сервіси перевіряють дозвіл ще раз і кидають `DomainException`.

| Роль | Меню |
|---|---|
| Пивовар | Рецепти · Партії · Склад (перегляд) |
| Помічник | Мої партії · Журнал вимірів · Розлив |
| Власник | Партії (перегляд) · Склад · Звіти · Користувачі |

Паролі зберігаються лише як хеші BCrypt. «Запам'ятати мене» — випадковий токен на 32 байти: у файлі `%LOCALAPPDATA%\BrewLog\session.json` лежить сам токен, а в БД лише його хеш. Вихід, скидання пароля й вимкнення запису відкликають токен. `UserService` не дає вимкнути чи понизити себе й стежить, щоб лишався хоча б один активний власник.

## BeerXML

Рецепти імпортуються й експортуються у форматі BeerXML 1.0 (`BeerXmlSerializer`, на `System.Xml.Linq`); числа у файлі завжди з крапкою, незалежно від культури системи.

- `FERMENTABLE`: `Grain` стає солодом-зерном, `Extract` і `Dry Extract` — солодом-екстрактом, `Sugar` і `Adjunct` — додатком.
- `HOP`: кілограми з файлу стають грамами, `Dry Hop` — сухим охмеленням, решта — кипінням.
- Стиль шукається за `CATEGORY_NUMBER` + `STYLE_LETTER`, а формула IBU зберігається в `IBU_METHOD`.
- Рядки прив'язуються до довідника за типом і назвою без урахування регістру; відсутня позиція створюється з характеристиками з файлу.
- Файл спершу перевіряється повністю, тому некоректний рецепт не лишає в БД половину імпорту.

Приклад файлу: [`docs/samples/american-ipa.beerxml.xml`](docs/samples/american-ipa.beerxml.xml).

## Склад

Склад складається з партій сировини. Для кожного типу інгредієнта є узагальнений `Stock<T>`, а `Inventory` тримає чотири такі склади й працює з рецептом.

- **FIFO.** Списання бере спершу партію з найближчим строком, за рівного строку — раніше отриману, партії без строку останні. Прострочені партії не списуються. Якщо сировини бракує, запуск варки відмовляє з переліком нестач.
- **Мінімуми.** Позиція нижче мінімуму позначається в таблиці й потрапляє в замовлення. Прострочені залишки в доступну кількість не входять.
- **Строки.** Партія придатна до кінця дати на упаковці; строк менше 30 днів дає статус «Спливає».
- **Потреба.** Для запланованих партій кількості рецепта множаться на масштаб `обсяг партії / обсяг рецепта`, а зведена таблиця показує нестачу й партії, яким бракує.
- **Замовлення.** Кількість — більша з нестач до мінімуму й до потреби, округлена вгору: кг до 0,5, г до 50, пакети до 1. Результат — PDF.
- **Ціна.** Собівартість рахується за середньозваженою ціною залишків. Списання фіксує в партії вартість сировини, яка була витрачена.

## Класи частини B для ЛР 1

| Клас | Роль | Файл |
|---|---|---|
| `Ingredient` | ієрархія, динамічний поліморфізм (`ApplyTo`) | `src/BrewLog.Domain/Recipes/Ingredient.cs` |
| `Malt`, `Hop`, `Yeast`, `Adjunct` | ієрархія: кожен тип сам додає внесок | `src/BrewLog.Domain/Recipes/` |
| `IngredientFactory` | патерн Factory | `src/BrewLog.Domain/Recipes/IngredientFactory.cs` |
| `IbuFormula` | ієрархія, патерн Strategy | `src/BrewLog.Domain/Ibu/IbuFormula.cs` |
| `TinsethFormula`, `RagerFormula` | стратегії розрахунку IBU | `src/BrewLog.Domain/Ibu/` |
| `IbuFormulaFactory` | патерн Factory | `src/BrewLog.Domain/Ibu/IbuFormulaFactory.cs` |
| `User` | ієрархія, динамічний поліморфізм (`Permissions`, `Sections`) | `src/BrewLog.Domain/Users/User.cs` |
| `Brewer`, `Assistant`, `Owner` | ієрархія: роль сама визначає дозволи й меню | `src/BrewLog.Domain/Users/` |
| `UserFactory` | патерн Factory | `src/BrewLog.Domain/Users/UserFactory.cs` |
| `Recipe` | інкапсуляція рядків, узагальнений метод `IngredientsOf<T>()` | `src/BrewLog.Domain/Recipes/Recipe.cs` |
| `RecipeStats`, `RecipeCalculator` | розрахунок показників рецепта | `src/BrewLog.Domain/Recipes/` |
| `BeerStyle`, `StyleCheck`, `SrmColor` | стиль BJCP, відповідність йому, колір пива | `src/BrewLog.Domain/Recipes/` |
| `Repository<T>` | узагальнений клас, статичний поліморфізм | `src/BrewLog.Data/Repositories/Repository.cs` |
| `StyleRepository`, `IngredientCatalogRepository`, `RecipeRepository`, `UserRepository` | нащадки `Repository<T>`, об'єкти створюють фабрики | `src/BrewLog.Data/Repositories/` |
| `RecipeService`, `CatalogService`, `AuthService`, `UserService` | сервіси: бізнес-логіка й перевірка дозволів | `src/BrewLog.Domain/Services/` |
| `BeerXmlSerializer` | обмін рецептами у форматі BeerXML | `src/BrewLog.Data/BeerXml/BeerXmlSerializer.cs` |
| `BCryptPasswordHasher` | хешування паролів і токена | `src/BrewLog.Data/Security/BCryptPasswordHasher.cs` |
| `ReferenceSectionViewModel<TEntity>` | узагальнений клас: спільна секція діалога «Довідники» | `src/BrewLog.UI/Features/References/ReferenceSectionViewModel.cs` |

## Класи частини A для ЛР 1

| Клас | Роль | Файл |
|---|---|---|
| `BatchState` | ієрархія, патерн State, динамічний поліморфізм (`CanAdvance`, `Next`, `OnEnter`) | `src/BrewLog.Domain/Batches/BatchState.cs` |
| `PlannedState`, `BrewingState`, `FermentingState`, `ConditioningState`, `PackagedState` | ієрархія: кожен етап сам вирішує, чи можна далі | `src/BrewLog.Domain/Batches/` |
| `BatchStateFactory` | патерн Factory | `src/BrewLog.Domain/Batches/BatchStateFactory.cs` |
| `Batch` | інкапсуляція вимірів і розливу, правила етапу | `src/BrewLog.Domain/Batches/Batch.cs` |
| `GravityStability`, `FermentationAnalyzer` | правило стабільності густини, попередження бродіння | `src/BrewLog.Domain/Batches/` |
| `BatchNumberGenerator`, `Packaging`, `Measurement` | нумерація партій, розлив, вимір | `src/BrewLog.Domain/Batches/` |
| `Stock<T>`, `StockEntry<T>` | узагальнені класи, статичний поліморфізм, FIFO-план | `src/BrewLog.Domain/Inventory/` |
| `Inventory`, `IStock`, `StockPosition` | увесь склад; побудова `Stock<T>` узагальненим методом | `src/BrewLog.Domain/Inventory/` |
| `StockLot`, `Supplier`, `PurchaseOrder`, `InsufficientStockException` | партія сировини, постачальник, замовлення, нестача | `src/BrewLog.Domain/Inventory/` |
| `InventoryService`, `BatchService`, `ReportService` | сервіси: бізнес-логіка й перевірка дозволів | `src/BrewLog.Domain/Services/` |
| `StockRepository`, `BatchRepository` | нащадки `Repository<T>`, зміни в транзакціях | `src/BrewLog.Data/Repositories/` |
| `PurchaseOrderPdf`, `LabelPdfGenerator`, `ReportPdfExporter` | документи PDF | `src/BrewLog.Data/Export/` |
| `StockExcelExporter`, `ReportExcelExporter` | експорт в Excel | `src/BrewLog.Data/Export/` |
| `StockSeeder`, `BatchSeeder` | демо-склад і демо-партії | `src/BrewLog.Data/Seed/` |

## Тести

120 тестових методів (335 випадків) у `tests/BrewLog.Tests`. Покриття рядків: Domain — 81,25 %, Data — 74,94 %. Очікувані значення — еталони з `HANDOFF.md` §6 або пораховані вручну; час у тестах фіксований.

| Тема | Методів |
|---|---|
| Рецепти, формули IBU, спільні типи (`Recipes`, `Ibu`, `Common`) | 30 |
| Партії: етапи, стабільність густини, аналізатор бродіння (`Batches`) | 17 |
| Склад: FIFO, залишки, замовлення (`Inventory`) | 12 |
| Сервіси й дозволи (`Services`, `Users`) | 32 |
| Звіти (`Reports`) | 4 |
| BeerXML (`BeerXml`) | 8 |
| SQLite, демо-дані, експорт Excel (`Data`) | 17 |

```bash
dotnet test
dotnet test --collect:"XPlat Code Coverage"
```

## Відповідність ЛР 1–3

Кількості пораховано синтаксичним розбором коду в `src` компілятором Roslyn. Автор — той, хто створив файл (`git log`), поділ збігається з `HANDOFF.md` §11.1. A — Maksym, B — Bilotserkovets Vladyslav.

| ЛР | Вимога | Є | A | B |
|---|---|---|---|---|
| 1 | Класи, ≥ 9 | 223 (`class` і `record`) | 98 | 125 |
| 1 | Поля, ≥ 15 | 454 | 227 | 227 |
| 1 | Нетривіальні методи, ≥ 25 (без автовластивостей, однорядкових геттерів і конструкторів) | 630 | 236 | 394 |
| 1 | Ієрархії, ≥ 2, одна з ≥ 3 класів | 4 | `BatchState` (5 станів) | `Ingredient` (4), `User` (3), `IbuFormula` (2) |
| 1 | Динамічний поліморфізм: перевизначення | 137 (80 методів, 57 властивостей) | 49 | 88 |
| 1 | Статичний поліморфізм: узагальнені класи | 5 | 2 | 3 |
| 1 | Статичний поліморфізм: узагальнені методи | 7 | 1 | 6 |
| 1 | Статичний поліморфізм: перевантаження | 1 група | 1 | 0 |
| 2 | Вікна, ≥ 4 | 13 | 6 | 7 |
| 2 | Сторінки й власні контроли | 9 | 6 | 3 |
| 2 | Контроли, ≥ 20 | 194 | 90 | 104 |
| 2 | Контейнери компонування | 253 | 128 | 125 |
| 2 | Обробники, ≥ 10 | 81 (56 команд, 19 підписок, 6 подій у XAML) | 34 | 47 |
| 2 | Логіка окремо від GUI | шари Domain, Data, UI | обидва | обидва |
| 3 | Зовнішні бібліотеки | 7, розділ «Бібліотеки» | ScottPlot, QuestPDF, ClosedXML | інфраструктура й BCrypt |
| 3 | Ретроспектива | `docs/RETRO.md` | сесії 1–2 і далі | сесії 1–4 |

## Документація

- [Опис проєкту](docs/BrewLog-опис.pdf)
- [План і архітектура](HANDOFF.md)

## Ретроспектива

Запис кожної робочої сесії: що зроблено, які були проблеми й що змінилось у плані — [`docs/RETRO.md`](docs/RETRO.md).

## Автори

| Автор (git log) | Частина | Внесок |
|---|---|---|
| Bilotserkovets Vladyslav (Kr11d) | B | фундамент і тема, рецепти з редактором, BeerXML, довідники, вхід, ролі й користувачі |
| Maksym (maksymchish) | A | склад, партії з етапами, журнал бродіння й графіки, розлив і етикетки, звіти й експорт, фінальна збірка |

## Використання ШІ

Під час розробки використовується Claude Code (Anthropic) як асистент. Історія запитів зберігається; весь код переглядається й розуміється авторами. Робочі домовленості з асистентом — у [`CLAUDE.md`](CLAUDE.md).
