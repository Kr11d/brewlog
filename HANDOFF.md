# BrewLog — HANDOFF

> Єдине джерело правди про проєкт для обох розробників і для агента (Claude Code).
> Опис проєкту лежить у `docs/BrewLog-опис.pdf`. Якщо HANDOFF і PDF розходяться, діє HANDOFF: тут зафіксовані уточнення, яких у PDF немає.
> Агент оновлює лише розділ **«13. Журнал стану»** (у кінці кожного промту). Решту розділів змінюйте тільки тоді, коли команда свідомо змінює рішення, і пишіть, що саме змінили.

---

## 0. Як почати (робить розробник B, ~20 хв)

1. Встановити: .NET 10 SDK, Git for Windows, Visual Studio (свіжа, з підтримкою .NET 10) або Rider, Claude Code.
2. Створити на GitHub порожній публічний репозиторій `brewlog`, додати розробника A (Максима) як collaborator.
3. Підключити до нього папку зі стартовими файлами (`git init`, `git remote add origin ...`):
   ```
   CLAUDE.md
   HANDOFF.md
   README.md
   docs/BrewLog-опис.pdf          ← PDF з описом проєкту
   docs/RETRO.md
   docs/prompts/PROMPTS_A.md
   docs/prompts/PROMPTS_B.md
   ```
4. `git add . && git commit -m "docs: стартові документи" && git push` у `main`.
5. Відкрити термінал у папці репозиторію, запустити `claude` і вставити промт **B-S** (старт сесії) з `PROMPTS_B.md`.

---

## 1. Що будуємо

**BrewLog** — настільний застосунок для обліку виробництва малої броварні: рецепти з живим розрахунком показників, партії з етапами, журнал бродіння з графіками й попередженнями, склад сировини, розлив з PDF-етикетками, звіти з експортом в Excel/PDF, обмін рецептами у форматі BeerXML. Три ролі: Пивовар, Помічник, Власник.

Курс «Парадигма ООП», лабораторні 1–3. Команда — 2 розробники: B (власник репозиторію) і A (Максим); код пише агент за промтами з `docs/prompts/`.

**Межі (свідомі спрощення):** один ПК і локальна SQLite, без сервера; виміри вручну; собівартість лише за сировиною; без бухгалтерії й акцизу; довідники стилів BJCP та інгредієнтів редагуються користувачем.

---

## 2. Ухвалені рішення

| Питання | Рішення |
|---|---|
| Мова / платформа | C# , **.NET 10 (LTS)**, WPF |
| Де працює агент | Claude Code **локально на Windows** у кожного розробника |
| Git | GitHub, **публічний репозиторій**, гілка на сесію + Pull Request у `main` |
| Графік роботи | **Естафета з однією передачею**: спершу **B** робить першу половину (B1–B12) і зливає в `main`, потім **A** робить другу (A1–A12). Одночасно працює одна людина |
| Поділ | **За функціями**: кожен веде свої фічі через усі шари (Domain → Data → UI) |
| MVVM | **CommunityToolkit.Mvvm** (поле + `[ObservableProperty]`, `[RelayCommand]`) |
| Вигляд | **Власна тема в стилі застосунку «Дія»** (не копія): градієнтний фон, білі заокруглені картки, великі заголовки, кнопки-«пігулки». Палітра **«Темне пиво»** (кремовий → теплий беж, акценти стаут-бордо) |
| Мова | Інтерфейс і коментарі — **українською**, ідентифікатори коду — **англійською** |
| Тести | **Повні, але прості**: 70–80 тестових методів xUnit на Domain, сервіси (власні заглушки), BeerXML і кілька перевірок на тимчасовій SQLite. Без бібліотек моків, без UI-тестів, без зовнішніх сервісів |
| Демо-дані | **Повний набір**: по користувачу на роль, стилі, інгредієнти, 5 рецептів, склад, ~8 партій із вимірами |
| Обсяг | **Усе з PDF**: 8 вікон, Tinseth + Rager, імпорт/експорт BeerXML, дошка партій, звіти в Excel і PDF, «Запам'ятати мене» |

---

## 3. Ролі та доступ

### 3.1 Розділи бічного меню

| Роль | Розділи (у такому порядку) | Домашній розділ |
|---|---|---|
| **Пивовар** (`Brewer`) | Рецепти · Партії · Склад (лише перегляд) | Рецепти |
| **Помічник** (`Assistant`) | Мої партії · Журнал вимірів · Розлив | Мої партії |
| **Власник** (`Owner`) | Партії (лише перегляд) · Склад · Звіти · Користувачі (відкриває діалог) | Партії |

### 3.2 Дозволи (`Permission`)

| Permission | Brewer | Assistant | Owner | Що означає |
|---|:-:|:-:|:-:|---|
| `ViewRecipes` | ✓ | | | бачити рецепти (помічник рецептів **не бачить**) |
| `EditRecipes` | ✓ | | | створювати/редагувати/видаляти, імпорт/експорт BeerXML |
| `EditReferences` | ✓ | | | редагувати довідники стилів і характеристик інгредієнтів |
| `ViewBatches` | ✓ | ✓ (свої) | ✓ | бачити партії; помічник — лише призначені йому або без виконавця |
| `ManageBatches` | ✓ | | | планувати партію, переводити між етапами, вводити фактичну OG |
| `AddMeasurements` | ✓ | ✓ | | вносити виміри густини й температури |
| `PackageBatches` | ✓ | ✓ | | оформлювати розлив, друкувати етикетки |
| `ViewStock` | ✓ | | ✓ | бачити склад |
| `ManageStock` | | | ✓ | прихід сировини, ціни, мінімальні запаси, замовлення постачальнику |
| `ViewReports` | | | ✓ | звіти й експорт |
| `ManageUsers` | | | ✓ | облікові записи |

Кнопки дій, на які немає дозволу, **приховані** (не просто вимкнені). Сервіси теж перевіряють дозвіл і кидають `DomainException`, якщо його немає.

### 3.3 Демо-облікові записи

| Логін | Пароль | Роль | Ім'я |
|---|---|---|---|
| `brewer` | `brewer123` | Пивовар | Андрій Солодовник |
| `assistant` | `assistant123` | Помічник | Марта Хмелевська |
| `owner` | `owner123` | Власник | Ірина Броварська |

Паролі — лише BCrypt-хеші (`BCrypt.Net-Next`).

---

## 4. Архітектура

### 4.1 Рішення й проєкти

```
BrewLog/
├─ BrewLog.sln (або .slnx — що створить dotnet new sln)
├─ src/
│  ├─ BrewLog.Domain/    net10.0          — класи предметної області, розрахунки, стани, стратегії, фабрики,
│  │                                       інтерфейси репозиторіїв і сервіси (бізнес-логіка). Без NuGet-залежностей.
│  ├─ BrewLog.Data/      net10.0          — SQLite через Dapper, схема й сідери, BeerXML, PDF (QuestPDF), Excel (ClosedXML),
│  │                                       BCrypt. Посилається на Domain.
│  └─ BrewLog.UI/        net10.0-windows  — WPF: вікна, сторінки, ViewModel, тема, навігація, композиція залежностей.
│                                          Посилається на Domain і Data.
├─ tests/
│  └─ BrewLog.Tests/     net10.0          — xUnit; посилається на Domain і Data (сервіси на власних заглушках, BeerXML, тимчасова SQLite).
├─ docs/  (PDF, RETRO.md, prompts/, samples/, screenshots/)
├─ .github/workflows/build.yml            — збірка + тести на windows-latest
├─ CLAUDE.md · HANDOFF.md · README.md
```

**Правила залежностей:** Domain нічого не знає про Data/UI/WPF/SQL. Вікна й сторінки не містять розрахунків і SQL — лише прив'язки до ViewModel. ViewModel викликає сервіси з Domain і (для файлів) експортери з Data.

### 4.2 NuGet-пакети (фіксований список — інших не додавати)

| Пакет | Проєкт | Для чого |
|---|---|---|
| `CommunityToolkit.Mvvm` | UI | MVVM: `ObservableObject`, `[ObservableProperty]`, `[RelayCommand]` |
| `ScottPlot.WPF` (v5) | UI | графіки бродіння й звітів, PNG графіка для PDF-звіту |
| `QuestPDF` | Data | етикетки, замовлення постачальнику, звіт у PDF |
| `ClosedXML` | Data | експорт звітів і складу в Excel |
| `Dapper`, `Microsoft.Data.Sqlite` | Data | база даних |
| `BCrypt.Net-Next` | Data | хешування паролів і токена «Запам'ятати мене» |
| `xunit`, `xunit.runner.visualstudio`, `Microsoft.NET.Test.Sdk`, `coverlet.collector` | Tests | тести й покриття |

### 4.3 Структура папок (namespace = шлях)

```
BrewLog.Domain/
  Common/        IEntity, DomainException, TransitionCheck, ValueRange
  Abstractions/  IRepository<T>, I*Repository, IPasswordHasher, ICurrentSession, IClock
  Recipes/       Ingredient, Malt, Hop, Yeast, Adjunct, IngredientKind, HopUse, IngredientData, IngredientFactory,
                 Recipe, RecipeStats, RecipeCalculator, BeerStyle, StyleCheck, SrmColor
  Ibu/           IbuFormula, TinsethFormula, RagerFormula, IbuFormulaFactory
  Inventory/     StockLot, StockEntry<T>, Stock<T>, Inventory, WriteOffPart, StockMovement, Supplier,
                 StockCheckResult, Shortage, PurchaseOrder, PurchaseOrderLine, InsufficientStockException
  Users/         User, Brewer, Assistant, Owner, UserRole, Permission, AppSection, UserFactory
  Batches/       Batch, BatchState, PlannedState, BrewingState, FermentingState, ConditioningState, PackagedState,
                 BatchStateFactory, TransitionContext, Measurement, GravityStability, FermentationAnalyzer,
                 FermentationAlert, AlertKind, Packaging, BatchNumberGenerator
  Reports/       MonthSummary, MonthlyVolume, RecipeCostRow, AbvComparisonRow
  Services/      RecipeService, CatalogService, InventoryService, AuthService, UserService, BatchService, ReportService
BrewLog.Data/
  Infrastructure/ SqliteConnectionFactory, DatabaseInitializer, AppPaths, Schema/schema.sql (EmbeddedResource)
  Repositories/   Repository<T> (абстрактний generic), UserRepository, StyleRepository, IngredientCatalogRepository,
                  RecipeRepository, StockRepository, BatchRepository
  Rows/           record-класи рядків таблиць (UserRow, RecipeIngredientRow, ...)
  Seed/           ISeeder, DemoSeeders, UserSeeder, CatalogSeeder, RecipeSeeder, StockSeeder, BatchSeeder
  Security/       BCryptPasswordHasher
  BeerXml/        BeerXmlSerializer
  Export/         PurchaseOrderPdf, StockExcelExporter, LabelPdfGenerator, ReportPdfExporter, ReportExcelExporter
BrewLog.UI/
  App.xaml(.cs), AppServices.cs (композиція залежностей), SmokeRunner.cs
  Themes/        Colors.xaml, Typography.xaml, Controls.xaml, DataGrid.xaml
  Controls/      спільні контроли (StatChip, StyleRangeBar, SectionCard ...)
  Converters/
  Services/      NavigationService, WindowService, DialogService, CurrentSession, RememberMeStore
  Shell/         MainWindow, MainViewModel, MenuItemViewModel
  Features/
    Login/ Users/ Recipes/ RecipeEditor/ References/         ← B
    Inventory/ Batches/ BatchCard/ Measurements/ Bottling/ Reports/      ← A
    Common/ PlaceholderPage, MessageDialog
```

### 4.4 Ключові механізми

- **Композиція залежностей** — власний клас `AppServices` (створюється в `App.OnStartup`): фабрика з'єднань → репозиторії → сервіси → `CurrentSession`, `NavigationService`, `WindowService`, `DialogService`. Без DI-контейнера (простіше пояснити на захисті).
- **Навігація** — `MainViewModel.CurrentPage` (ViewModel сторінки) + `DataTemplate DataType=...` в `App.xaml` зіставляє ViewModel → UserControl. Меню будується з `CurrentSession.User.Sections`.
- **Окремі вікна (Window):** Вхід, Оболонка (MainWindow), Редактор рецепта, Картка партії, Розлив, Користувачі (діалог), Довідники (діалог), Прихід сировини (діалог), Нова партія (діалог), MessageDialog. Відкриваються через `WindowService`.
- **Сторінки (UserControl у MainWindow):** Рецепти, Партії / Мої партії, Журнал вимірів, Розлив (список), Склад, Звіти.
- **Сесія** — `ICurrentSession` (Domain/Abstractions), реалізація `CurrentSession` в UI. До промту B5 стоїть заглушка `AllowAllSession` (усе дозволено).
- **Smoke-перевірка** — `BrewLog.UI.exe --smoke`: створює тимчасову БД, сідить, для кожної ролі створює кожну сторінку і кожне вікно (без показу), пише результат у `%TEMP%\brewlog-smoke.log`, виходить з кодом 0/1. Ловить помилки XAML і ресурсів, які інакше видно лише під час запуску. Кожне нове вікно чи сторінку додають у список `SmokeRunner`.
- **Файл БД:** `%LOCALAPPDATA%\BrewLog\brewlog.db`. Шлях передається в конструктор `SqliteConnectionFactory`: smoke і тести підставляють тимчасовий файл. Якщо файлу немає — створюється схема й запускаються сідери. Змінили схему або сідери → видаліть файл (агент нагадує про це в журналі стану).
- **QuestPDF:** на старті `QuestPDF.Settings.License = LicenseType.Community;`. Для кирилиці явно задавати шрифт, наприклад `Segoe UI`.
- **Dapper:** `Dapper.DefaultTypeMap.MatchNamesWithUnderscores = true;`, кожне з'єднання виконує `PRAGMA foreign_keys = ON;`. Дати — `TEXT` в ISO-8601 (`DateTime`, без `DateOnly`). Гроші — `decimal` у C#, `REAL` у БД.

---

## 5. Доменна модель і ООП

### 5.1 Чотири ієрархії (вимога ЛР 1)

**1. `Ingredient` (abstract)** — `Malt`, `Hop`, `Yeast`, `Adjunct`
- Спільне: `Id`, `CatalogId`, `Name`, `Amount`; `abstract IngredientKind Kind`, `abstract string Unit` («кг» / «г» / «пак.»).
- `abstract void ApplyTo(RecipeStats stats)` — **кожен тип сам додає свій внесок**:
  - `Malt` (`YieldPercent`, `ColorLovibond`, `IsMashed`) — екстракт (з урахуванням ефективності, якщо `IsMashed`) і колір (MCU);
  - `Adjunct` (`YieldPercent`) — екстракт без ефективності, кольору не додає;
  - `Hop` (`AlphaPercent`, `TimeMin`, `Use: Boil|DryHop`) — реєструє варильне внесення для IBU; `DryHop` гіркоти **не додає**;
  - `Yeast` (`AttenuationPercent`, `TempMinC`, `TempMaxC`) — задає ступінь зброджування й температурний діапазон.
- `virtual string DescribeContribution(RecipeStats final)` — текст внеску рядка для редактора («+0,031 OG · 4,2 SRM», «18,4 IBU»).
- `virtual IngredientData ToData()` / `abstract Ingredient Clone()`.
- Одиниці: солод і додатки — **кг**, хміль — **г**, дріжджі — **пакети**. Ціна на складі — грн за цю одиницю.

**2. `BatchState` (abstract, патерн State)** — `PlannedState → BrewingState → FermentingState → ConditioningState → PackagedState`
- `Code` (`Planned`…), `DisplayName` («Заплановано», «Варка», «Бродіння», «Дозрівання», «Розлито»), `Order`, `virtual bool AllowsMeasurements`.
- `abstract TransitionCheck CanAdvance(TransitionContext ctx)` — **кожен стан сам вирішує**, чи можна далі; `abstract BatchState Next()`; `virtual void OnEnter(Batch b, DateTime now)` — ставить мітку часу етапу.
- `TransitionContext(Batch Batch, DateTime Now, StockCheckResult? Stock)`; `TransitionCheck(bool Allowed, string? Reason)`.

**3. `User` (abstract)** — `Brewer`, `Assistant`, `Owner`
- `Id`, `Login`, `FullName`, `PasswordHash`, `IsActive`, `RememberTokenHash`; `abstract UserRole Role`, `abstract string RoleDisplayName`, `abstract IReadOnlySet<Permission> Permissions`, `abstract IReadOnlyList<AppSection> Sections`, `virtual AppSection HomeSection`, `bool Can(Permission p)`.

**4. `IbuFormula` (abstract, патерн Strategy)** — `TinsethFormula` (за замовчуванням), `RagerFormula`
- `abstract string Code`, `abstract string DisplayName`, `abstract double Calculate(Hop hop, double boilGravity, double volumeL)`.
- Рецепт зберігає код формули; `IbuFormulaFactory.FromCode(code)`.

### 5.2 Узагальнені класи (статичний поліморфізм)

- `Repository<T> where T : class, IEntity` (Data) — абстрактна база з `GetById`, `GetAll`, `Insert`, `Update`, `Delete` на Dapper; конкретні репозиторії наслідуються й перевизначають SQL/мапінг.
- `Stock<T> where T : Ingredient` і `StockEntry<T>` (Domain) — склад одного типу інгредієнта: залишки, мінімуми, строки придатності, FIFO-план списання. `Inventory` тримає `Stock<Malt>`, `Stock<Hop>`, `Stock<Yeast>`, `Stock<Adjunct>`.
- `Recipe.IngredientsOf<T>() where T : Ingredient` — вибірка рядків потрібного типу.

### 5.3 Патерни

- **State** — етапи партії (5.1, п. 2).
- **Strategy** — формула IBU (5.1, п. 4).
- **Factory** — `IngredientFactory` створює інгредієнт потрібного класу з `IngredientData` (рядок БД, BeerXML, вибір у редакторі); `BatchStateFactory`, `UserFactory`, `IbuFormulaFactory`. **`switch` за типом/кодом дозволений лише всередині фабрик**; у розрахунках — тільки поліморфізм.

### 5.4 Інші ключові класи

| Клас | Відповідальність |
|---|---|
| `Recipe` | назва, стиль, обсяг партії (л), ефективність (%), час кипіння, код формули IBU, нотатки, список `Ingredient`; `AddIngredient`, `RemoveIngredient`, `ScaleFactor(volumeL)` |
| `RecipeStats` | вхід: обсяг, ефективність; накопичувачі: пункти екстракту, MCU, внесення хмелю, ступінь зброджування, діапазон температур; результат: `Og`, `Fg`, `Abv`, `Ibu`, `Srm`, `Ebc`; `Finish(IbuFormula)` |
| `RecipeCalculator` | `Calculate(Recipe)`: новий `RecipeStats` → `ingredient.ApplyTo(stats)` для кожного → `stats.Finish(formula)` |
| `BeerStyle` + `StyleCheck` | діапазони BJCP (OG, FG, ABV, IBU, SRM) як `ValueRange`; перевірка: кожен параметр «нижче / в межах / вище» + позиція 0..1 для шкали |
| `SrmColor` | SRM → RGB для кольорової плашки: `R = 255·0,975^SRM, G = 255·0,88^SRM, B = 255·0,7^SRM` |
| `Batch` | номер `BL-2026-001`, рецепт (id + знімок назви й стилю), обсяг, стан, дати етапів, планові OG/FG/ABV/SRM (знімок), фактична OG, собівартість сировини, діапазон температур дріжджів (знімок), виконавець-помічник, виміри, розлив; `Advance(ctx)`, `AddMeasurement(...)`, `ActualAbv` |
| `GravityStability` | правило «густина стабільна 3 дні» (див. 6.3) |
| `FermentationAnalyzer` | попередження: температура поза діапазоном, зупинка бродіння, готовність до дозрівання, немає вимірів > 2 днів |
| `Packaging` | пляшки (к-сть × обсяг), кеги (к-сть × обсяг), кінцева густина, розлитий обсяг, втрати |
| `InventoryService` | залишки, прихід, ціни, `CheckAvailability`, `WriteOffForBatch` (FIFO), потреба для запланованих варок, замовлення постачальнику, оцінка собівартості |

---

## 6. Розрахунки (еталонні значення — для тестів)

Константа екстракту сахарози: **385,6** пункта·л/кг (46,21 PPG × 8,345).

### 6.1 Рецепт

| Показник | Формула | Еталон |
|---|---|---|
| Пункти екстракту | `Σ кг × Yield/100 × 385,6 × (IsMashed ? Ефективність/100 : 1) / Обсяг_л` | 5 кг Pilsner (80 %, затирається), ефективність 72 %, 23 л → **48,3** пункта |
| OG | `1 + пункти / 1000` | → **1,0483** |
| Додаток (цукор) | те саме без ефективності, колір 0 | 0,5 кг сахарози (100 %), 23 л → **+8,38** пункта |
| FG | `1 + (OG − 1) · (1 − A)`, A — максимальне зброджування серед дріжджів, без дріжджів 0,75 | OG 1,050, A = 75 % → **1,0125** |
| ABV, % | `(OG − FG) · 131,25` | → **4,92** |
| IBU Tinseth | `1,65 · 0,000125^(G − 1) · (1 − e^(−0,04·t)) / 4,15 · (α · грами · 1000 / л)` | 30 г, α = 10 %, 60 хв, 23 л, G = 1,050 → **30,1** |
| IBU Rager | `U = (18,11 + 13,86 · tanh((t − 31,32)/18,27))/100`; `GA = G > 1,050 ? (G − 1,050)/0,2 : 0`; `IBU = грами · U · α · 1000 / (л · (1 + GA))` | ті самі дані → **40,2** |
| IBU Rager, висока густина | ті самі дані, G = 1,070: GA = 0,1 | → **36,5** |
| Сухе охмелення | 0 IBU | → **0** |
| MCU | `Σ (кг · 2,20462 · °L) / (л · 0,264172)` | 5 кг 1,8 °L + 0,3 кг 60 °L, 23 л → **9,80** |
| SRM (Morey) | `1,4922 · MCU^0,6859` | → **7,14** |
| EBC | `SRM · 1,97` | → **14,06** |
| Собівартість, грн/л | `Σ кількість · ціна одиниці / обсяг`; ціна — середньозважена по залишках партій складу, якщо залишків немає — остання ціна з довідника | — |

`G` (густина кипіння) = OG рецепта. `α` — частка (0,10), а не відсотки.

### 6.2 Партія

- Масштаб: `k = Batch.VolumeL / Recipe.BatchVolumeL`; потреба сировини = кількість у рецепті × k.
- Фактична ABV = `(ActualOg − Packaging.FinalGravity) · 131,25`.
- Втрати розливу = `VolumeL − (пляшки·обсяг + кеги·обсяг)`, не менше 0; розлитий обсяг не може перевищувати обсяг партії.

### 6.3 Правила переходів етапів

| Зі стану | Умова переходу далі | Що відбувається |
|---|---|---|
| Заплановано → Варка | вистачає сировини (`ctx.Stock.IsSufficient`; перевіряє `BatchService` через `InventoryService`) | списання сировини FIFO, `IngredientsCost` = сума списаного |
| Варка → Бродіння | введено фактичну OG | `FermentingAt` |
| Бродіння → Дозрівання | `GravityStability.IsStable`: **останні 3 виміри густини зроблені в 3 різні дні, охоплюють ≥ 2 доби, max − min ≤ 0,001** | `ConditioningAt` |
| Дозрівання → Розлито | існує запис `Packaging` (оформлюється у вікні «Розлив») | `PackagedAt` |
| Розлито | ніколи («Етап фінальний») | — |

Виміри дозволені в станах Бродіння й Дозрівання.

Уточнення до `GravityStability`:
- Дні рахуються за календарними датами (`DateTime.Date`), час доби не враховується. Виміри лише з температурою пропускаються.
- Густину порівнювати з допуском: `max - min <= 0.001 + 1e-9`. У `double` 1,012 − 1,011 = 0,0010000000000001119, і проста перевірка хибно поверне «нестабільно».

### 6.4 Попередження бродіння (`FermentationAnalyzer`)

| Тип | Умова | Рівень |
|---|---|---|
| `TemperatureOutOfRange` | остання температура поза `[TempMin; TempMax]` дріжджів | помилка |
| `Stalled` | густина стабільна, але `> PlannedFg + 0,004` | попередження |
| `ReadyForConditioning` | густина стабільна і `≤ PlannedFg + 0,004` | інформація |
| `NoRecentMeasurement` | у стані Бродіння немає вимірів понад 2 доби | попередження |

Межу `PlannedFg + 0,004` теж порівнювати з допуском `1e-9`. Для `NoRecentMeasurement` відлік іде від останнього виміру, а якщо їх немає — від `FermentingAt`.

---

## 7. Схема БД (`src/BrewLog.Data/Infrastructure/Schema/schema.sql`)

```sql
CREATE TABLE IF NOT EXISTS users (
  id INTEGER PRIMARY KEY AUTOINCREMENT,
  login TEXT NOT NULL UNIQUE,
  full_name TEXT NOT NULL,
  role TEXT NOT NULL CHECK (role IN ('Brewer','Assistant','Owner')),
  password_hash TEXT NOT NULL,
  is_active INTEGER NOT NULL DEFAULT 1,
  remember_token_hash TEXT NULL,
  created_at TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS styles (
  id INTEGER PRIMARY KEY AUTOINCREMENT,
  code TEXT NOT NULL UNIQUE,            -- '21A'
  name TEXT NOT NULL,                   -- 'American IPA'
  category TEXT NOT NULL,
  og_min REAL NOT NULL, og_max REAL NOT NULL,
  fg_min REAL NOT NULL, fg_max REAL NOT NULL,
  abv_min REAL NOT NULL, abv_max REAL NOT NULL,
  ibu_min REAL NOT NULL, ibu_max REAL NOT NULL,
  srm_min REAL NOT NULL, srm_max REAL NOT NULL
);

CREATE TABLE IF NOT EXISTS ingredient_catalog (
  id INTEGER PRIMARY KEY AUTOINCREMENT,
  kind TEXT NOT NULL CHECK (kind IN ('Malt','Hop','Yeast','Adjunct')),
  name TEXT NOT NULL,
  yield_percent REAL NULL,              -- Malt, Adjunct
  color_lovibond REAL NULL,             -- Malt
  is_mashed INTEGER NULL,               -- Malt: 1 — зерно (з ефективністю), 0 — екстракт
  alpha_percent REAL NULL,              -- Hop
  attenuation_percent REAL NULL,        -- Yeast
  temp_min_c REAL NULL, temp_max_c REAL NULL,  -- Yeast
  min_stock REAL NOT NULL DEFAULT 0,    -- в одиницях складу (кг / г / пак.)
  last_price REAL NOT NULL DEFAULT 0,   -- грн за одиницю
  notes TEXT NULL,
  UNIQUE (kind, name)
);

CREATE TABLE IF NOT EXISTS recipes (
  id INTEGER PRIMARY KEY AUTOINCREMENT,
  name TEXT NOT NULL,
  style_id INTEGER NULL REFERENCES styles(id),
  batch_volume_l REAL NOT NULL,
  efficiency_percent REAL NOT NULL DEFAULT 72,
  boil_time_min INTEGER NOT NULL DEFAULT 60,
  ibu_formula TEXT NOT NULL DEFAULT 'Tinseth' CHECK (ibu_formula IN ('Tinseth','Rager')),
  notes TEXT NULL,
  author_id INTEGER NULL REFERENCES users(id),
  created_at TEXT NOT NULL,
  updated_at TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS recipe_ingredients (
  id INTEGER PRIMARY KEY AUTOINCREMENT,
  recipe_id INTEGER NOT NULL REFERENCES recipes(id) ON DELETE CASCADE,
  catalog_id INTEGER NOT NULL REFERENCES ingredient_catalog(id),
  kind TEXT NOT NULL CHECK (kind IN ('Malt','Hop','Yeast','Adjunct')),
  name TEXT NOT NULL,
  amount REAL NOT NULL,                 -- кг (Malt, Adjunct), г (Hop), пак. (Yeast)
  yield_percent REAL NULL, color_lovibond REAL NULL, is_mashed INTEGER NULL,
  alpha_percent REAL NULL, hop_time_min REAL NULL,
  hop_use TEXT NULL CHECK (hop_use IN ('Boil','DryHop')),
  attenuation_percent REAL NULL, temp_min_c REAL NULL, temp_max_c REAL NULL,
  sort_order INTEGER NOT NULL DEFAULT 0
);

CREATE TABLE IF NOT EXISTS suppliers (
  id INTEGER PRIMARY KEY AUTOINCREMENT,
  name TEXT NOT NULL UNIQUE,
  phone TEXT NULL,
  email TEXT NULL
);

CREATE TABLE IF NOT EXISTS stock_lots (
  id INTEGER PRIMARY KEY AUTOINCREMENT,
  catalog_id INTEGER NOT NULL REFERENCES ingredient_catalog(id),
  supplier_id INTEGER NULL REFERENCES suppliers(id),
  quantity_received REAL NOT NULL,
  quantity_left REAL NOT NULL,
  unit_price REAL NOT NULL,
  received_at TEXT NOT NULL,
  expires_at TEXT NULL,
  lot_code TEXT NULL
);

CREATE TABLE IF NOT EXISTS batches (
  id INTEGER PRIMARY KEY AUTOINCREMENT,
  number TEXT NOT NULL UNIQUE,          -- BL-2026-001
  recipe_id INTEGER NOT NULL REFERENCES recipes(id),
  recipe_name TEXT NOT NULL,
  style_name TEXT NULL,                 -- знімок: етикетка не потребує ViewRecipes
  volume_l REAL NOT NULL,
  state TEXT NOT NULL CHECK (state IN ('Planned','Brewing','Fermenting','Conditioning','Packaged')),
  planned_date TEXT NOT NULL,
  brewed_at TEXT NULL, fermenting_at TEXT NULL, conditioning_at TEXT NULL, packaged_at TEXT NULL,
  planned_og REAL NOT NULL, planned_fg REAL NOT NULL, planned_abv REAL NOT NULL, planned_srm REAL NOT NULL,
  temp_min_c REAL NULL, temp_max_c REAL NULL,
  actual_og REAL NULL,
  ingredients_cost REAL NULL,           -- грн, сума списаної сировини
  assistant_id INTEGER NULL REFERENCES users(id),
  created_by INTEGER NULL REFERENCES users(id),
  notes TEXT NULL
);

CREATE TABLE IF NOT EXISTS stock_movements (
  id INTEGER PRIMARY KEY AUTOINCREMENT,
  lot_id INTEGER NOT NULL REFERENCES stock_lots(id),
  batch_id INTEGER NULL REFERENCES batches(id),
  kind TEXT NOT NULL CHECK (kind IN ('Receipt','WriteOff','Adjustment')),
  quantity REAL NOT NULL,               -- > 0 прихід, < 0 списання
  unit_price REAL NOT NULL,
  created_at TEXT NOT NULL,
  user_id INTEGER NULL REFERENCES users(id)
);

CREATE TABLE IF NOT EXISTS measurements (
  id INTEGER PRIMARY KEY AUTOINCREMENT,
  batch_id INTEGER NOT NULL REFERENCES batches(id) ON DELETE CASCADE,
  measured_at TEXT NOT NULL,
  gravity REAL NULL,
  temperature_c REAL NULL,
  note TEXT NULL,
  user_id INTEGER NULL REFERENCES users(id)
);

CREATE TABLE IF NOT EXISTS packagings (
  id INTEGER PRIMARY KEY AUTOINCREMENT,
  batch_id INTEGER NOT NULL UNIQUE REFERENCES batches(id) ON DELETE CASCADE,
  packaged_at TEXT NOT NULL,
  bottles_count INTEGER NOT NULL DEFAULT 0,
  bottle_volume_l REAL NOT NULL DEFAULT 0.5,
  kegs_count INTEGER NOT NULL DEFAULT 0,
  keg_volume_l REAL NOT NULL DEFAULT 20,
  final_gravity REAL NULL,
  loss_l REAL NOT NULL DEFAULT 0,
  user_id INTEGER NULL REFERENCES users(id),
  notes TEXT NULL
);
```

Індекси за потреби (`measurements(batch_id)`, `stock_lots(catalog_id)`, `recipe_ingredients(recipe_id)`). Схему створює B у B1 одразу для всіх таблиць — A лише користується нею.

---

## 8. Контракти між A і B

Сигнатури можна уточнювати, але **не перейменовувати й не видаляти** після того, як ними почав користуватися інший розробник. Будь-яку зміну — в «Журнал стану».

```csharp
// --- B1: Domain/Abstractions ---
public interface IEntity { int Id { get; set; } }
public interface IRepository<T> where T : class, IEntity
{ T? GetById(int id); IReadOnlyList<T> GetAll(); int Insert(T e); void Update(T e); void Delete(int id); }
public interface ICurrentSession            // B1 — заглушка AllowAllSession; B5 — справжня реалізація
{ User? User { get; } bool IsAuthenticated { get; } bool Can(Permission p);
  IReadOnlyList<AppSection> Sections { get; } event EventHandler? Changed; }
public interface IClock { DateTime Now { get; } }   // SystemClock; у тестах — фіксована дата
public enum AppSection { Recipes, Batches, MyBatches, Measurements, Bottling, Inventory, Reports, Users }
public enum Permission { ViewRecipes, EditRecipes, EditReferences, ViewBatches, ManageBatches,
                         AddMeasurements, PackageBatches, ViewStock, ManageStock, ViewReports, ManageUsers }
// User у B1 — мінімальний абстрактний клас (Id, Login, FullName); B5 доповнює ієрархію.

// --- B3/B4: рецепти (користується A) ---
public sealed class RecipeService
{ IReadOnlyList<Recipe> GetAll(); Recipe? Get(int id); RecipeStats Calculate(Recipe r);
  int Save(Recipe r); void Delete(int id); Recipe Duplicate(int id); decimal EstimateCostPerLiter(Recipe r); }

// --- A1: склад (користується BatchService в A3) ---
public sealed class InventoryService
{ StockCheckResult CheckAvailability(Recipe recipe, double volumeL);
  decimal WriteOffForBatch(int batchId, Recipe recipe, double volumeL, int? userId); // FIFO, повертає вартість
  /* решта методів, зокрема Requirement(recipeId, volumeL), — для сторінки складу (A7) */ }
public sealed record StockCheckResult(bool IsSufficient, IReadOnlyList<Shortage> Shortages);
public sealed record Shortage(string Name, string Unit, double Required, double Available);

// --- B5: користувачі (користується A) ---
// ICurrentSession.Can(Permission.EditRecipes) тощо; User.Sections; AuthService.Login/Logout.
// UserService.GetAssistants() — активні помічники для вибору виконавця партії (Permission.ManageBatches).

// --- A3: партії (A7 бере звідси планові партії для «потреби сировини») ---
public sealed class BatchService
{ IReadOnlyList<Batch> GetAll(); IReadOnlyList<Batch> GetPlanned(); IReadOnlyList<Batch> GetForAssistant(int userId); Batch? Get(int id);
  Batch Plan(int recipeId, double volumeL, DateTime plannedDate, int? assistantId);
  TransitionCheck CanAdvance(int batchId); void Advance(int batchId);
  void SetActualOg(int batchId, double og); Measurement AddMeasurement(int batchId, DateTime at, double? gravity, double? tempC, string? note);
  void Package(int batchId, Packaging p); IReadOnlyList<FermentationAlert> GetAlerts(int batchId); }

// --- UI (B2): WindowService / DialogService / NavigationService ---
// Кожен додає свої методи у WindowService: ShowRecipeEditor(int? id), ShowBatchCard(int id), ShowBottling(int id) ...
// DialogService: ShowInfo, ShowError, Confirm — через стилізоване MessageDialog.
```

**Сідери** (`ISeeder { int Order { get; } void Seed(IDbConnection c, IDbTransaction tx, DateTime today); }`; потрібні залежності, як-от `IPasswordHasher` для `UserSeeder`, — через конструктор сідера; список сідерів — у `Data/Seed/DemoSeeders.All(IPasswordHasher)`), запускаються за `Order` лише для нової БД:
`10 UserSeeder (B)` → `20 CatalogSeeder (B)` → `30 RecipeSeeder (B)` → `40 StockSeeder (A)` → `50 BatchSeeder (A)`.
Демо-дати рахуються від `today` (графіки завжди «свіжі»).

---

## 9. Інтерфейс

### 9.1 Принципи стилю (натхнення «Дією», але своє)

- Фон вікна — м'який діагональний градієнт; контент — на **білих картках** з радіусом 20–24 і ледь помітною тінню.
- Багато повітря: відступи 24/16/8, жодних сірих рамок навколо всього підряд.
- Великий заголовок сторінки (30–32, SemiBold) + короткий сірий підзаголовок.
- Основні дії — **темні кнопки-«пігулки»** (висота 48, радіус 24); другорядні — білі з тонкою рамкою; третинні — текстові.
- Поля вводу — високі (48), заокруглені (14), заливка `Surface.Muted`, без рамки; у фокусі — рамка `Accent`.
- Списки — як у Дії: рядки-картки зі стрілкою `›` справа; таблиці (DataGrid) — без сітки, рядок 44, заголовки дрібним сірим.
- **«Документ-картка»**: заголовок картки партії й прев'ю рецепта — градієнтна картка (бурштин → стаут) з білим текстом: номер, назва, стиль, ABV, етап.
- Статуси — чипи з м'яким тонованим фоном.
- Шрифт — `Segoe UI Variable Text` / `Segoe UI Variable Display` з резервом `Segoe UI`; іконки — `Segoe Fluent Icons` з резервом `Segoe MDL2 Assets`.
- Бічне меню — біла заокруглена панель зліва; активний пункт — темна «пігулка» з білим текстом; унизу — ім'я, роль і «Вийти».
- Ніяких кольорів «у лоб» у розмітці — лише ключі ресурсів теми.

### 9.2 Палітра «Темне пиво» (`Themes/Colors.xaml`)

| Ключ | Колір | Де |
|---|---|---|
| `Bg.GradientStart` / `Bg.GradientEnd` | `#FBF7F1` → `#EFE3D3` | фон вікон |
| `Surface` | `#FFFFFF` | картки |
| `Surface.Muted` | `#F5EEE4` | поля вводу, вторинні панелі |
| `Border` | `#E7DCCD` | тонкі рамки |
| `Text.Primary` / `Text.Secondary` | `#23150F` / `#7A665A` | текст |
| `Accent` / `Accent.Hover` / `Accent.Soft` | `#5E1A28` / `#4A1420` / `#F2E2E2` | основні кнопки, активне меню, виділення |
| `Amber` | `#B9772B` | другорядний акцент, графіки, «документ-картка» (початок градієнта) |
| `Success` / `Warning` / `Danger` / `Info` | `#2F7D57` / `#B7791F` / `#B42318` / `#3B5B8C` | статуси, попередження |
| `Stage.Planned` · `Brewing` · `Fermenting` · `Conditioning` · `Packaged` | `#8C7B70` · `#B9772B` · `#3B5B8C` · `#6A4C93` · `#2F7D57` | чипи й колонки дошки |
| `DocCard.Gradient` | `#B9772B` → `#5E1A28` | «документ-картка» |

### 9.3 Вікна й сторінки

| # | Екран | Тип | Власник | Зміст |
|---|---|---|---|---|
| 1 | Вхід | Window | B | логотип «BrewLog», логін, пароль, «Запам'ятати мене», «Увійти» |
| — | Оболонка | Window | B | бічне меню за роллю, область сторінки, профіль і «Вийти» |
| 2 | Рецепти | сторінка | B | таблиця (назва, стиль, OG, FG, ABV, IBU, колір-плашка, грн/л), пошук, фільтр за стилем, панель прев'ю, кнопки: новий, редагувати, дублювати, видалити, імпорт/експорт BeerXML, «Довідники» |
| 3 | Редактор рецепта | Window | B | параметри рецепта, таблиці солоду, хмелю, дріжджів і додатків із внеском кожного рядка, вибір стилю й формули IBU, живі показники, шкали відповідності стилю |
| 4 | Партії / Мої партії | сторінка | A | дошка (5 колонок за етапами) або список, перемикач; «Запланувати партію» (діалог); для помічника — лише свої |
| 5 | Картка партії | Window | A | «документ-картка», крокова шкала етапів, графіки густини й температури (ScottPlot), форма нового виміру, попередження, таблиця вимірів, кнопка переходу з причиною відмови, введення фактичної OG |
| 6 | Розлив | сторінка + Window | A | сторінка: партії в стані «Дозрівання»; вікно: пляшки й кеги, кінцева густина, втрати, «Підтвердити розлив», «Друк етикеток» (PDF) |
| 7 | Склад | сторінка | A | залишки з фільтрами за типом, позначки «нижче мінімуму» / «спливає строк», потреба для запланованих варок, «Прихід сировини» (діалог), «Замовлення постачальнику» (PDF), «Експорт в Excel» |
| 8 | Звіти | сторінка | A | показники місяця (плитки), графік обсягу за 12 місяців, собівартість за рецептами, планова vs фактична ABV, експорт в Excel і PDF |
| — | Журнал вимірів | сторінка | A | активні партії помічника з останніми вимірами й попередженнями, швидка форма нового виміру |
| — | Користувачі | діалог | B | список, додати, змінити ім'я/роль, скинути пароль, деактивувати (не себе; має лишитися ≥ 1 активний власник) |
| — | Довідники | діалог | B | вкладки «Стилі BJCP» і «Інгредієнти» (характеристики); ціни й мінімуми редагує власник у «Складі» |

Обробники подій для ЛР 2: команди `RelayCommand` + події в code-behind там, де WPF не дає прив'язки (`PasswordBox.PasswordChanged`, перемальовування ScottPlot, `Loaded`). У README рахуємо обидва види.

---

## 10. Демо-дані

- **Користувачі** — таблиця 3.3.
- **Стилі (BJCP 2021, діапазони з настанов BJCP):** 1B American Lager, 3B Czech Premium Pale Lager, 4A Munich Helles, 5D German Pils, 10A Weissbier, 11A Ordinary Bitter, 12C English IPA, 15B Irish Stout, 16A Sweet Stout, 18B American Pale Ale, 19A American Amber Ale, 20A American Porter, 21A American IPA, 21C Hazy IPA, 24A Witbier.
- **Інгредієнти (~25):** солоди — Pilsner, Pale Ale, Munich, Vienna, Wheat, Carapils, Crystal 60, Chocolate, Roasted Barley, вівсяні пластівці; хміль — Saaz, Hallertauer Mittelfrüh, East Kent Goldings, Magnum, Cascade, Citra, Mosaic; дріжджі — US-05, S-04, W-34/70, WB-06, Nottingham; додатки — сахароза, декстроза. Реалістичні характеристики, мінімальні запаси й ціни в грн.
- **Постачальники:** 2 вигадані.
- **Рецепти (5):** Czech Pils (3B), American IPA (21A, сухе охмелення), Irish Stout (15B), Weissbier (10A), American Pale Ale (18B, формула Rager). Показники мають потрапляти в стиль.
- **Склад:** партії сировини на більшість позицій; 2–3 позиції нижче мінімуму, 2 зі строком < 30 днів, 1 прострочена. **Citra і Mosaic — мало** (менше, ніж потрібно для American IPA на 50 л), решти для неї вистачає.
- **Партії (~8, за останні 6 місяців):** 4 «Розлито» (з розливом і собівартістю), 1 «Дозрівання», 2 «Бродіння» (одна з виходом температури за межі, одна з 3 днями стабільної густини — «готова до дозрівання»), 1 «Заплановано» — **American IPA, 50 л, через 3 дні** (для неї бракує Citra і Mosaic → видно на складі в «Потребі» і в перевірці при запуску варки). Виміри — щодня, реалістична крива падіння густини. Собівартість засіяних партій записується напряму, без руху складу.
- **Собівартість літра партії** (для звітів) = `IngredientsCost / розлитий обсяг`.

---

## 11. Поділ робіт

### 11.1 Хто чим володіє

| | **Розробник B** (власник репозиторію) — перша половина: «Фундамент, рецепти, користувачі» | **Розробник A** (Максим) — друга половина: «Склад, партії, звіти» |
|---|---|---|
| Domain | Common, Abstractions, Recipes, Ibu, Users; RecipeService, CatalogService, AuthService, UserService | Inventory, Batches, Reports; InventoryService, BatchService, ReportService |
| Data | Infrastructure, schema.sql, `Repository<T>`, User/Style/IngredientCatalog/Recipe репозиторії, BCryptPasswordHasher, BeerXml, `DemoSeeders`, User/Catalog/Recipe сідери | Stock/Batch репозиторії, PurchaseOrderPdf, StockExcelExporter, LabelPdfGenerator, ReportPdfExporter, ReportExcelExporter, Stock/Batch сідери |
| UI | App, AppServices, Themes, Controls, Shell, Navigation/Window/DialogService, SmokeRunner, CurrentSession, RememberMeStore; Features: Login, Users, Recipes, RecipeEditor, References, Common | Features: Inventory, Batches, BatchCard, Measurements, Bottling, Reports |
| Тести | ValueRange, схема БД, розрахунки рецепта, IBU, інгредієнти, ролі, Auth, UserService, RecipeService, CatalogService, BeerXML, демо-рецепти | склад FIFO, InventoryService, етапи партії, стабільність густини, аналізатор бродіння, BatchService, звіти, Excel-експорт, демо-склад і демо-партії |
| Документи | README (розділи B), RETRO (свої записи) | README (фінал, таблиця ЛР), RETRO (свої записи) |

**Спільні файли** (може змінювати будь-хто, мінімально й акуратно): `App.xaml`, `AppServices.cs`, `WindowService.cs`, `SmokeRunner.cs`, `Data/Seed/DemoSeeders.cs`, `README.md`, `HANDOFF.md` §13, `docs/RETRO.md`.

Після передачі A може мінімально правити файли B, якщо без цього ніяк, із записом у журнал стану.

### 11.2 Графік: естафета з однією передачею

| Крок | Хто | Промт | Зміст |
|---|---|---|---|
| 1 | B | B1 | Фундамент рішення й БД |
| 2 | B | B2 | Тема й оболонка |
| 3 | B | B3 | Домен рецептів і формули |
| 4 | B | B4 | Дані рецептів, довідники, демо-рецепти |
| 5 | B | B5 | Користувачі, ролі, автентифікація |
| 6 | B | B6 | Вхід, «Запам'ятати мене», меню за роллю |
| 7 | B | B7 | Діалог «Користувачі» |
| 8 | B | B8 | Сторінка «Рецепти» |
| 9 | B | B9 | Редактор рецепта |
| 10 | B | B10 | BeerXML |
| 11 | B | B11 | Довідники |
| 12 | B | B12 | Полірування й **передача A** |
| 13 | A | A1 | Склад: домен, дані, сервіс |
| 14 | A | A2 | Домен партій і бродіння |
| 15 | A | A3 | Дані партій і BatchService |
| 16 | A | A4 | Дошка партій |
| 17 | A | A5 | Картка партії з графіками |
| 18 | A | A6 | Журнал вимірів і «Мої партії» |
| 19 | A | A7 | Сторінка «Склад» і прихід |
| 20 | A | A8 | Замовлення в PDF і Excel складу |
| 21 | A | A9 | Розлив і етикетки |
| 22 | A | A10 | Сторінка «Звіти» |
| 23 | A | A11 | Експорт звітів |
| 24 | A | A12 | Фінал: README, ЛР, перевірка |

**Чому саме такий порядок:** перша половина самодостатня: розділи партій, складу й звітів поки показують `PlaceholderPage`. Друга спирається на неї: A1 потребує рецептів і каталогу; A2 використовує `StockCheckResult` з A1; A3 потребує `RecipeService`, `InventoryService` і домену партій; A7 бере планові партії з A3; A11 використовує `ReportService` з A10. У кожного є своє ООП-ядро для захисту: у B — ієрархії Ingredient, IbuFormula, User; у A — BatchState і `Stock<T>`. **Не міняйте порядок промтів.**

### 11.3 Git

- Перед сесією: `git checkout main && git pull`, нова гілка `b/s1`, `b/s2` … або `a/s1`, `a/s2` …
- Один промт = щонайменше один коміт: `feat(recipes): ...`, `fix(batches): ...`, `test: ...`, `style(ui): ...`, `docs: ...`.
- Кінець сесії: push, PR `[B] Сесія 1 — фундамент`, перевірка (build + test + smoke). Автор сам зливає PR у `main` після зеленого CI, щоб естафета не чекала. Фінальний PR розробника A (промт A12) перед злиттям переглядає B як власник репозиторію.
- Коміти робляться від імені розробника (його git-ідентичність) — це і є авторство для README. Агент додає в кінець повідомлення `Co-Authored-By` зі своєю моделлю.
- Репозиторій публічний: у `user.email` цього репозиторію ставте адресу GitHub типу noreply (`ID+логін@users.noreply.github.com`), щоб особиста пошта не потрапила в історію.

---

## 12. Відповідність ЛР (перевіряється в A12)

| ЛР | Вимога | Як закрито |
|---|---|---|
| 1 | ≥ 9 класів, ≥ 15 полів, ≥ 25 нетривіальних методів | ~45 класів у Domain/Data; розрахунки OG/FG/IBU/SRM, переходи етапів, FIFO-списання, BeerXML, аналізатор бродіння… |
| 1 | ≥ 2 ієрархії, одна з ≥ 3 класів | 4 ієрархії: Ingredient (4), BatchState (5), User (3), IbuFormula (2) |
| 1 | ≥ 3 випадки поліморфізму, статичний і динамічний | динамічний: `Ingredient.ApplyTo`, `BatchState.CanAdvance/Next`, `IbuFormula.Calculate`, `User.Sections`; статичний: `Repository<T>`, `Stock<T> where T : Ingredient`, `IngredientsOf<T>()`, перевантаження |
| 2 | ≥ 4 вікна, ≥ 20 контролів, контейнер, ≥ 10 обробників | 10 Window + 6 сторінок; Grid/DockPanel/ItemsControl; команди й події |
| 2 | Логіка окремо від GUI | Domain / Data / UI, MVVM |
| 3 | Зовнішні бібліотеки, ретроспектива | 7 бібліотек (§4.2), `docs/RETRO.md` |

---

## 13. Журнал стану

> Агент додає рядок **наприкінці кожного промту**. Найновіше — внизу.
> Якщо змінилась схема або сідери — напишіть «⚠ видаліть `%LOCALAPPDATA%\BrewLog\brewlog.db`».

| Дата | Хто | Промт | Зроблено | Далі / відомі проблеми |
|---|---|---|---|---|
| 2026-11-06 | B | — | Стартові документи оновлено: графік змінено на естафету (спершу B, потім A), промти перенумеровано й перерозподілено (§11), тести розширено до 70–80 методів, уточнено §6.3–6.4, додано `style_name` і `planned_srm` у `batches`, `UserService.GetAssistants`, заборону видалення рецепта з партіями | — |
| 2026-11-06 | B | — | Створено публічний репозиторій, додано README і правила охайності в `CLAUDE.md` | B: промт B-S, потім B1 |
| 2026-11-06 | B | B1 | Рішення `BrewLog.slnx` із 4 проєктами; Domain (Common, Abstractions, мінімальний `User`), Data (схема §7, `DatabaseInitializer`, `Repository<T>`, порожні `DemoSeeders`), UI (App, `AppServices`, `AllowAllSession`, тимчасове `MainWindow`, `SmokeRunner`), тести, CI, «Запуск» у README | Далі B2. Уточнення: `IPasswordHasher` = `Hash(password)` / `Verify(password, hash)` (у §8 сигнатур не було). `BCryptPasswordHasher` зроблено вже тут, бо він потрібен для `DemoSeeders.All`; у B5 лише використати. Поліморфні репозиторії перевизначають `Repository<T>.Load`. B2: глобальний обробник у `App.xaml.cs` поки показує `MessageBox`, перевести на `DialogService`. У `BrewLog.UI.csproj` вимкнено NU1701: ScottPlot.WPF на `net10.0-windows` бере збірку SkiaSharp для net462, рендер графіка перевірено |
| 2026-11-06 | B | B2 | Тема «Темне пиво» (Colors, Typography, Controls, DataGrid), оболонка з бічним меню, `NavigationService`, `WindowService`, `DialogService`, `MessageDialog`, `PlaceholderPage`. Глобальний обробник збоїв показує `MessageDialog`, а системне вікно лишилось резервом. `SmokeRunner` тепер застосовує шаблони й сам перебирає сторінки всіх розділів | Далі B3. Список стилів — на початку `Themes/Controls.xaml`. Нова сторінка: фабрика в `AppServices.CreatePageFactories` + DataTemplate в `App.xaml`. Нове вікно: метод у `WindowService` + рядок у `SmokeRunner.Targets`. Колонкам `CleanDataGrid` задавайте явну ширину (`*` або `Column.Number`): `Auto` поруч із `*` обрізає значення. Якщо перший `dotnet build` після додавання чи видалення XAML падає з CS2001 або CS0103 `InitializeComponent`, повторіть збірку: імовірно, заважає фонова збірка мовного сервера VS Code в тому самому `obj` (поза робочою папкою VS Code не відтворилось) |
| 2026-11-07 | B | B3 | Домен рецептів: ієрархії `Ingredient` і `IbuFormula`, фабрики, `RecipeStats`, `RecipeCalculator`, `Recipe`, `BeerStyle`, `StyleCheck`, `SrmColor`; тести на еталони §6.1 | Далі B4. Рішення, яких не було в HANDOFF: температурний діапазон рецепта беруть дріжджі з найбільшим зброджуванням (`RecipeStats.TempMinC/TempMaxC`, знадобиться A для знімка в партії); частка SRM рядка пропорційна його MCU. `Recipe.IbuFormulaCode` — це колонка `ibu_formula`. `Ingredient.Validate` вимагає кількість > 0, тому для довідника (Amount = 0) у B4 треба відкрити окрему перевірку характеристик |
| 2026-11-07 | B | B4 | Репозиторії стилів, довідника й рецептів, `CatalogService`, `RecipeService`, сідери (15 стилів за сторінками bjcp.org, 24 інгредієнти, 2 постачальники, 5 рецептів на 50 л), діагностика рецептів у smoke | ⚠ видаліть `%LOCALAPPDATA%\BrewLog\brewlog.db`. Далі B5. Для всіх сервісів є `ICurrentSession.Require(Permission)` і `DomainException.ThrowIfAny(errors)`. Читання довідників не потребує дозволу: вони потрібні і для рецептів, і для складу. `Ingredient` не несе `min_stock` і `last_price`, а `IngredientCatalogRepository.Update` їх не змінює: ціни дає `GetLastPrices`, мінімуми й ціни редагує A. Зброджування дріжджів у демо-довіднику фактичне, нижче за паспортне, бо інакше лагер і пшеничне не потрапляють у FG свого стилю. `Ingredient.ValidateCatalogItem` перевіряє позицію довідника без кількості |
| 2026-11-07 | B | B-E | Сесія 1 завершена: готові B1–B4 (фундамент, тема й оболонка, домен рецептів, дані рецептів і демо-дані); 50 тестових методів | Наступний промт — B5 у новій сесії `b/s2`, після злиття PR. ⚠ `brewlog.db` видаліть, якщо ще не робили цього після B4; B5 знову змінить сідери |
| 2026-11-07 | B | B5 | Ієрархія `User` (`Brewer`, `Assistant`, `Owner`) з дозволами й розділами за §3.1–3.2, `UserFactory`, `UserRepository`, `AuthService` (вхід, токен «Запам'ятати мене»), `UserService` з `GetAssistants`, `CurrentSession` замість `AllowAllSession`, `UserSeeder`; 66 тестових методів | ⚠ видаліть `%LOCALAPPDATA%\BrewLog\brewlog.db`. Далі B6. До B6 звичайний запуск показує оболонку без меню, бо ніхто не увійшов; smoke сам входить кожним демо-користувачем і лишає в сесії пивовара. Уточнення до §8: `LoginWithToken` повертає `User?` (null — токен недійсний або запис вимкнено); «Logout» — це `RevokeRememberToken` + `CurrentSession.SignOut()`; `AppServices.Session` тепер типу `CurrentSession`, `Auth` і `Users` — нові властивості. Роль змінює `User.WithRole`: створює об'єкт іншого класу. `ResetPassword` і вимкнення запису відкликають токен. Сесія тримає об'єкт, отриманий під час входу: якщо в B7 власник змінить своє ім'я, оновіть сесію повторним `SignIn`. Сесію виконано в Linux-контейнері: WPF тут лише компілюється, тож `--smoke` треба запустити на Windows |
| 2026-11-07 | B | B6 | Вікно входу (`LoginWindow`, `LoginViewModel`), `RememberMeStore` (`session.json`), автовхід за токеном під час запуску, оболонка реагує на `CurrentSession.Changed` (меню й профіль за роллю, старт із `HomeSection`), «Вийти» повертає до входу; smoke перевіряє вікно входу, а для кожної ролі — оболонку й сторінки з її меню | Далі B7. Вхід і головне вікно змінюють одне одного через `WindowService` у порядку «показати нове → закрити старе», `ShutdownMode = OnLastWindowClose`. Нові методи `WindowService`: `CreateLoginWindow`, `ShowLoginWindow`, `ReturnToLogin`. Оболонка після `Changed` лишає відкриту сторінку, якщо її розділ є в новому меню: повторний `SignIn` після зміни власного імені в B7 оновить профіль без переходу. Нові стилі теми: `LoginWindow`, `LogoMark.Large`, `ErrorText` (порожній текст ховається). Smoke не запускався (Linux-контейнер), ключі ресурсів у XAML перевірено скриптом; запустіть `--smoke` на Windows |
| 2026-11-07 | B | B7 | Діалог «Користувачі» (`UsersDialog`, `UsersViewModel`): список рядків-карток із пошуком, ініціалами, чипом ролі й позначкою «вимкнено», форма запису (ім'я, логін лише для нового, роль, перемикач «Активний», паролі для нового), «Скинути пароль» — окремий `ResetPasswordDialog`. Відкривається з меню власника через `WindowService.ShowUsers()`; smoke перевіряє обидва стани форми й діалог пароля | Далі B8. Правила лишаються в `UserService`; форма сама перевіряє лише вибір ролі й збіг паролів. Збереження наявного запису — два виклики (`Update`, потім `SetActive`), тож після помилки список перечитується з БД, а введене у формі лишається. Зміна власного запису оновлює сесію повторним `SignIn`. Нові стилі теми: `DialogWindow`, `ListRowCard.Selectable` (`IsSelected`, `IsMuted`), `ToggleSwitch`, `Chip.Accent`, `FieldLabel`, `Glyph.Key`. Smoke не запускався (Linux-контейнер), ключі ресурсів перевірено скриптом |
| 2026-11-07 | B | B8 | Сторінка «Рецепти» (`RecipesPage`, `RecipesViewModel`): таблиця з пошуком, фільтром стилю й сортуванням (числа праворуч, кольорова плашка через `SrmToBrushConverter`), прев'ю з «документ-карткою», складом за групами й нотатками, дії «Новий / Редагувати / Дублювати / Видалити», порожні стани; кнопки без дозволу приховані | Далі B9: замінити заглушку `WindowService.ShowRecipeEditor(int? id)` (зараз показує «Буде в B9» і повертає false); сторінка вже перечитує дані й лишає вибраним збережений рецепт. Формати чисел для всіх сторінок — `UI/Formatting/DisplayFormat` (густина, ABV, цілі, гроші, кількість з одиницею, узгодження «1 рецепт / 2 рецепти / 5 рецептів»). Головне вікно відкривається розгорнутим: за ширини 1280 таблиця з прев'ю не вміщується й прокручується по горизонталі. Нові ресурси теми: `Swatch`, `Swatch.Large`, `HeroNumber`, `SecondaryButton.Compact`, `CleanDataGrid.Text` / `.Number` / `.NumberHeader`, `Column.Narrow`, `Column.MinText`, `Width.PreviewPane`, `Width.Filter`. Smoke не запускався (Linux-контейнер), ключі ресурсів перевірено скриптом |
| 2026-11-07 | B | B-E | Сесія 2 завершена: готові B5–B8 (користувачі, ролі й автентифікація; вхід, «Запам'ятати мене» й меню за роллю; діалог «Користувачі»; сторінка «Рецепти»); 66 тестових методів (177 випадків) | Наступний промт — B9 у новій сесії `b/s3` після злиття PR. ⚠ видаліть `%LOCALAPPDATA%\BrewLog\brewlog.db`, якщо ще не робили цього після B5 (додано сідер користувачів). Smoke у цій сесії не запускався (Linux-контейнер): перед злиттям запустіть `--smoke` на Windows і пройдіть чек-листи B6–B8 |
| 2026-11-07 | B | B9 | Вікно «Редактор рецепта» (`RecipeEditorWindow`, `RecipeEditorViewModel`, класи рядків за типами, `IngredientSectionViewModel`, спливний вибір `CatalogPicker`): параметри, чотири таблиці, живі показники через `RecipeService.Calculate`, шкали `Controls/StyleRangeBar`; smoke перевіряє новий рецепт, редагування й вибір. Виправлено шаблон `InputCombo`: вибране значення показувало `ToString()` запису замість `DisplayMemberPath`, це стосувалось і фільтра стилів, і вибору ролі | Далі B10. `brewlog.db` видаляти не треба: Domain, схема й сідери не змінювались. Уточнення до §8: `ShowRecipeEditor(int? id, out int savedId)`, бо сторінка має вибрати щойно створений рецепт; `CreateRecipeEditor(int? id)` — для smoke. Числові форми (B11, A): `Features/Common/NumberField` зі стилями `NumberBox` і `NumberBox.Cell`. Поле тримає текст, бо прив'язка до `double` з оновленням на кожну клавішу не дає надрукувати кому; `DisplayFormat.TryParseNumber` приймає кому й крапку в межах 1e-6…1e9. Таблиці-форми: `CleanDataGrid.Form` (поля в `DataGridTemplateColumn`, колесо миші передає `Controls/WheelForwarding`). Інші нові ресурси теми: `EditorWindow`, `InputCombo.Cell`, `IconButton.Cell`, `PickerOption`, `StyleRangeBar`, `Caption.StyleRange`, `CleanDataGrid.Caption`, `Glyph.Delete`, `Column.Input`, `Column.Wide`, `Column.Contribution`, `Column.Icon`. Закриття вікна зі змінами питає підтвердження, тому не змінюйте дані редактора перед `Close()` у smoke: діалог заблокує перевірку. На B12: у прев'ю сторінки «Рецепти» написи «Редагувати» й «Дублювати» обрізаються в кнопках |
| 2026-11-07 | B | B10 | `Data/BeerXml/BeerXmlSerializer` (BeerXML 1.0 на `System.Xml.Linq`), `RecipeService.Import(path)` і `Export(recipeIds, path)`, кнопки імпорту й експорту на сторінці «Рецепти», зразок `docs/samples/american-ipa.beerxml.xml`; 77 тестових методів (208 випадків) | Далі B11. `brewlog.db` видаляти не треба. Конструктор `RecipeService` отримав `IBeerXmlSerializer` (Domain/Abstractions), бо Domain не знає про Data. Рішення поза промтом: формула зберігається в стандартному полі `IBU_METHOD`, інакше рецепт із Rager після імпорту змінив би IBU; `AMOUNT` дріжджів — кількість пакетів, а значення менше 1 (літри чи кілограми в чужих файлах) дає 1 пакет; `Domain/Common/NumberLimits` — спільні межі чисел для полів форми й файлу. Імпорт спершу перевіряє весь файл, тож некоректний рецепт не лишає часткових даних; спільної транзакції між репозиторіями немає, тому збій самої БД посеред імпорту ще може лишити частину. Для всіх: `DialogService.PickFileToOpen` і `PickFileToSave` (згодяться A для PDF і Excel). ⚠ Тестів уже 77 при орієнтирі 70–80 на весь проєкт, а A ще не почав: переглянути в аудиті B12 |
| 2026-11-07 | B | B-E | Сесія 3 завершена: готові B9–B10 (редактор рецепта з живим розрахунком; імпорт і експорт BeerXML); 77 тестових методів (208 випадків) | Наступний промт — B11 у новій сесії `b/s4` після злиття PR. `brewlog.db` видаляти не треба: схема й сідери в цій сесії не змінювались. Build, test і smoke зелені на Windows. Перед злиттям пройдіть чек-листи B9 і B10: живий поп-ап, фокус, Tab, Esc, колесо миші й системні вікна вибору файлу автоматично не перевірялись |
| 2026-11-07 | B | B11 | Діалог «Довідники» (`ReferencesDialog`, `ReferencesViewModel`): вкладки стилів та інгредієнтів на спільному узагальненому `ReferenceSectionViewModel<TEntity>`; форма характеристик — окремий клас і `DataTemplate` на кожен тип, без `switch`; ціни й мінімуми лише для читання. Сторінка «Рецепти» перечитує дані після закриття; smoke перевіряє обидві вкладки й кожен тип; 79 тестових методів (218 випадків) | Далі B12. `brewlog.db` видаляти не треба: схема й сідери не змінювались. Рішення поза промтом: `BeerStyle.Validate` вимагає категорію й перевіряє межі (OG 1,000–1,200, FG 0,990–1,100, ABV 0–25, IBU 0–150, SRM 0–100); перехід на інший рядок, «Новий» і закриття зі змінами питають підтвердження, а перемикання типу чи вкладки нічого не скидає, бо кожна секція тримає власну форму. Для A: `CatalogService.GetLastPrices()` і `GetMinStocks()` (новий `IIngredientCatalogRepository.GetMinStocks()`), `NumberField` має необов'язковий `format`. Виправлено тему: `TabItem` більше не задає собі `Foreground` і `FontWeight`, бо їх успадковував вміст вкладки (білий текст на білій картці), а відступ перенесено в шаблон, бо `Margin` вкладки `TabPanel` обрізав. Нові ресурси: `Width.StyleCode`, `Width.RangeLabel`. На B12: Esc і Ctrl+S у цьому діалозі ще немає; після збою шаблону smoke лишає вікно відкритим, тому наступні збої можуть бути наслідком першого, а процес падає без рядка «РЕЗУЛЬТАТ» (код виходу все одно ненульовий); ⚠ тестів уже 79 при орієнтирі 70–80 на весь проєкт. Сценарії й вигляд перевірено тимчасовим скриптом поза репозиторієм (поза екраном, зі скріншотами); живі клік мишею, фокус і Tab — за чек-листом B11 |
| 2026-11-07 | B | B12 | **Передача A.** Готово B1–B12: фундамент, тема, рецепти з редактором, BeerXML, довідники, вхід і ролі, користувачі. Полірування: Ctrl+F, Esc і Enter на екранах, ToolTip, числа в таблиці рецептів праворуч і без обрізання, незбережені зміни в «Користувачах», smoke без каскаду збоїв, README частини B. Аудит тестів: 76 методів (216 випадків). `brewlog.db` засівається з нуля, smoke зелений | **Далі — A1.** `brewlog.db` видаляти не треба. **Контракти §8, що вже працюють:** `IEntity`, `IRepository<T>`, `ICurrentSession`, `IClock`; `RecipeService` (ще й `Import`, `Export`); `CatalogService`; `AuthService`, `UserService.GetAssistants`, `User.Sections`; `NavigationService`, `WindowService`, `DialogService`; `ISeeder`, `DemoSeeders.All`. Уточнення: `ShowRecipeEditor(int? id, out int savedId)`, `IPasswordHasher` = `Hash` / `Verify`. **Заглушки меню (`PlaceholderPage`):** Партії, Мої партії, Журнал вимірів, Розлив, Склад, Звіти; таблиці складу й партій порожні, постачальники засіяні. **Що знати A.** A1: `EstimateCostPerLiter` поки бере лише `last_price`; переведи на ціну за §6.1 і онови речення про ціну в README («Рецепти й розрахунки»). `Ingredient` не несе `min_stock` і `last_price`, а `IngredientCatalogRepository.Update` їх не змінює: читати можна через `CatalogService.GetLastPrices` і `GetMinStocks`, а запис додає A; нова позиція довідника має 0 і 0. Нова сторінка: фабрика в `AppServices.CreatePageFactories` + `DataTemplate` в `App.xaml`. Нове вікно: метод у `WindowService` + рядок у `SmokeRunner.Targets`; вміст невибраної вкладки чи шаблон типу smoke бачить, лише якщо створити вікно саме в цьому стані. Клавіші: стиль `SearchBox` сам дає Ctrl+F, `DialogWindow` сам закриває на Esc (для незбережених змін — підтвердження в `Closing`), Enter і Ctrl+S — `KeyBinding` у `InputBindings` картки форми, а не `IsDefault`, щоб Enter у пошуку нічого не зберігав; у `EditorWindow` Esc і Ctrl+S — власні `KeyBinding`. Таблиці: числовим колонкам поруч із зірочковими задавай `MinWidth` (`Column.MinNarrow`, `Column.MinNumber`), інакше у вузькому вікні числа обрізаються; `Column.MinText` тепер 120. `SecondaryButton.Compact` має відступ 8. Вміст `TabItem` тепер не успадковує колір вкладки, тож `TabControl` безпечний. Реальна ширина: на екрані 1920×1080 із масштабом 125 % вікно має 1536 логічних пікселів. Сідери `StockSeeder` (40) і `BatchSeeder` (50) додавай у `DemoSeeders.All`. **Відомі обмеження:** імпорт BeerXML спершу перевіряє весь файл, але спільної транзакції між репозиторіями немає, тому збій самої БД посеред імпорту може лишити частину даних. ⚠ Тести: сценарії промтів A дадуть орієнтовно ще 30–35 методів, тобто разом близько 110 при орієнтирі 70–80; дублікати в частині B прибрано, тож орієнтир варто переглянути всією командою, а не різати сценарії. Живі клік мишею, фокус, Tab і системні вікна вибору файлу автоматично не перевірялись: пройди сценарій демонстрації з README |
| 2026-11-07 | B | B-E | Сесія 4 завершена: готові B11–B12 (діалог «Довідники»; полірування й передача), отже перша половина B1–B12 завершена; 76 тестових методів (216 випадків) | Наступний промт — A1 у сесії розробника A (`a/s1`) після злиття PR. `brewlog.db` видаляти не треба: схема й сідери в цій сесії не змінювались. Build, test і smoke зелені на Windows. Перед злиттям пройдіть сценарій демонстрації з README і заповніть «Мої висновки» в RETRO: живі клавіші, клік мишею й системні вікна вибору файлу автоматично не перевірялись |
