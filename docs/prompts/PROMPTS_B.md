# Промти розробника B — перша половина: «Фундамент, рецепти, користувачі»

> Ти відповідаєш за фундамент рішення, тему інтерфейсу, рецепти з розрахунками, вхід і ролі, користувачів, редактор рецепта, BeerXML і довідники. Також ти власник репозиторію.
> Деталі — `HANDOFF.md`, правила для агента — `CLAUDE.md` (агент читає його сам).

## Як працювати з промтами

1. **Порядок:** ти працюєш **першим**. Виконай B1–B12 по порядку, злий усе в `main` і передай проєкт розробнику A (Максиму).
2. Відкрий термінал у папці репозиторію → `claude` → вставляй промти **по одному, по порядку**: `B-S` → промти → `B-E`. Роботу можна розбити на кілька сесій: кожна починається з `B-S` і закінчується `B-E`.
3. Копіюй **увесь** текст із сірого блоку. Чекай, поки агент закінчить (він сам збере, протестує й закомітить).
4. Після кожного промту пройдись коротким чек-листом **«Перевір сам»**. Знайшов проблему — опиши її агенту звичайними словами (можна вставити скріншот) або візьми «рятувальний» промт у кінці файлу.
5. Між промтами можна робити `/clear`: пам'ять проєкту — у `HANDOFF.md` (журнал стану) і git, `CLAUDE.md` підтягується автоматично. Після `/clear` достатньо написати «Продовжуємо, наступний промт:» і вставити його.
6. Для великих промтів (B1, B2, B6, B9) за бажанням увімкни режим планування (`Shift+Tab`), переглянь план і підтверди.

| Етап | Промти | Що буде в кінці |
|---|---|---|
| 1. Фундамент | B1 · B2 | рішення, БД, CI, тема, оболонка з меню |
| 2. Рецепти: логіка | B3 · B4 | усі формули з тестами, довідники, демо-рецепти |
| 3. Користувачі | B5 · B6 · B7 | ролі, вхід із «Запам'ятати мене», меню за роллю, керування користувачами |
| 4. Рецепти: екрани | B8 · B9 · B10 · B11 | сторінка рецептів, редактор із живим розрахунком, BeerXML, довідники |
| 5. Передача | B12 | полірування, README частини B, нотатка для A |

## B-S · Старт сесії (на початку кожної сесії, заміни N)

```text
Починаємо сесію. Я — розробник B, сесія N за docs/prompts/PROMPTS_B.md.
1. git checkout main && git pull.
2. Прочитай CLAUDE.md і HANDOFF.md повністю; особливо §8 (контракти), §11 (поділ і графік) і §13 (журнал стану).
3. git log --oneline -15 — подивись останні зміни.
4. Створи гілку b/sN.
5. dotnet build і dotnet test (якщо рішення вже існує) — переконайся, що main зелений. Якщо ні — зупинись і скажи мені, що зламано. Якщо в журналі є «⚠ видаліть brewlog.db» — нагадай мені.
6. Відповідай коротко (5–8 рядків): стан проєкту, який промт наступний, чи є ризики. Код поки не пиши.
```

## B-E · Фініш сесії (наприкінці кожної сесії)

```text
Завершуємо сесію.
1. dotnet build, dotnet test, dotnet run --project src/BrewLog.UI -- --smoke — усе має бути зелене. Якщо ні — виправ.
2. Перевір, що в HANDOFF §13 є рядки за кожен промт цієї сесії. Додай підсумковий рядок: що готово, який промт наступний, чи треба видалити brewlog.db.
3. Допиши в docs/RETRO.md запис «Сесія N — Розробник B — <дата>» за шаблоном: заповни «Зроблено», «Проблеми й рішення», «Що змінили в плані»; розділ «Мої висновки» залиш із порожніми пунктами — я заповню сам.
4. Закоміть, git push -u origin b/sN.
5. Якщо встановлено gh — створи PR у main з назвою «[B] Сесія N — <коротко>» і описом (список зробленого + що перевірити). Якщо gh немає — дай мені готові назву й опис PR.
6. Наостанок — 3–5 рядків: що змінилось і на що звернути увагу далі.
```

**Після B-E (руками):** дочекайся зеленого CI на GitHub → **Merge**.
---

# Етап 1. Фундамент

## B1 · Фундамент рішення й база даних

```text
Промт B1 — фундамент рішення і бази даних. Прочитай CLAUDE.md і HANDOFF.md §4, §5.2, §7, §8.

1. Рішення BrewLog з проєктами (структура — HANDOFF §4.1):
   - src/BrewLog.Domain (net10.0), src/BrewLog.Data (net10.0), src/BrewLog.UI (net10.0-windows, WPF, WinExe), tests/BrewLog.Tests (xUnit, net10.0).
   - Посилання: Data → Domain; UI → Domain, Data; Tests → Domain, Data.
   - Directory.Build.props: Nullable enable, ImplicitUsings enable, LangVersion latest.
2. NuGet (останні стабільні): Data — Dapper, Microsoft.Data.Sqlite, QuestPDF, ClosedXML, BCrypt.Net-Next; UI — CommunityToolkit.Mvvm, ScottPlot.WPF; Tests — пакети шаблону xunit. Domain — без пакетів.
3. Domain:
   - Common: IEntity, DomainException, TransitionCheck (record: Allowed, Reason; статичні Ok() і Deny(reason)), ValueRange (record struct: Min, Max, Contains, Position(value) → 0..1 з обмеженням).
   - Abstractions: IRepository<T>, ICurrentSession, IClock + SystemClock, IPasswordHasher — сигнатури з HANDOFF §8.
   - Users: enum AppSection, enum Permission (HANDOFF §8); мінімальний абстрактний User (Id, Login, FullName, abstract Sections, abstract Can(Permission)) з коментарем «ієрархію доробляє розробник B у B5».
4. Data:
   - Infrastructure/AppPaths: папка %LOCALAPPDATA%\BrewLog і типовий шлях до БД, без статичної підміни. Шлях передається в конструктор SqliteConnectionFactory: smoke і тести підставляють свій.
   - SqliteConnectionFactory: повертає відкрите з'єднання й виконує PRAGMA foreign_keys = ON.
   - Infrastructure/Schema/schema.sql — ПОВНІСТЮ з HANDOFF §7 (+ індекси), EmbeddedResource.
   - DatabaseInitializer (фабрика з'єднань і список сідерів — через конструктор): якщо файлу БД немає — створити, виконати схему, потім усі ISeeder за Order в одній транзакції. Тут же Dapper.DefaultTypeMap.MatchNamesWithUnderscores = true.
   - Seed/ISeeder (HANDOFF §8). Список сідерів — Seed/DemoSeeders.All(IPasswordHasher) (поки порожній, з коментарем про порядок 10–50); ним користуються AppServices, SmokeRunner і тести.
   - Repositories/Repository<T> where T : class, IEntity — абстрактний generic на Dapper: TableName, GetById, GetAll, Delete реалізовані; Insert, Update — abstract; захищені хелпери Query/QuerySingle/Execute. XML-коментар: «узагальнений клас — статичний поліморфізм (ЛР 1)».
5. UI:
   - App.xaml / App.xaml.cs: культура uk-UA для потоку й WPF (FrameworkElement.LanguageProperty), QuestPDF.Settings.License = LicenseType.Community, глобальний обробник необроблених винятків (лог у %LOCALAPPDATA%\BrewLog\logs + зрозуміле повідомлення).
   - AppServices: композиція (фабрика з'єднань, ініціалізатор, SystemClock, заглушка AllowAllSession : ICurrentSession — усе дозволено, усі розділи).
   - Тимчасове MainWindow з написом «BrewLog» (оболонку зробимо в B2).
   - SmokeRunner: аргумент --smoke → тимчасова БД у %TEMP%, ініціалізація із сідерами, список «що створити» (поки MainWindow), лог у %TEMP%/brewlog-smoke.log (кожен крок + винятки повністю), Environment.Exit(0/1), запобіжник 60 с.
6. Тести (папки в tests/BrewLog.Tests відповідають темам: Common/, Data/, Fakes/):
   - Fakes/: FixedClock : IClock і FakeSession : ICurrentSession із заданими дозволами й користувачем. Ними користуватимуться всі тести сервісів.
   - Data/TempDatabase: власний тимчасовий файл БД на кожен тест, Pooling=False, видалення в Dispose, список сідерів через параметр.
   - Сценарії: ValueRange.Contains із межами ([Theory]); ValueRange.Position обмежує значення 0..1, а при Min = Max не ділить на нуль ([Theory]); нова БД має всі 11 таблиць і ввімкнений foreign_keys.
7. .gitignore (VS, Rider, bin/obj, TestResults/, *.db, *.user, .claude/settings.local.json), .gitattributes (* text=auto), .github/workflows/build.yml (windows-latest, actions/setup-dotnet 10.0.x, restore, build, test).
8. README.md уже існує: додай лише «Запуск», бо він уже працює. Решту розділів додають B12 і A12: без плейсхолдерів і порожніх заголовків.

Наприкінці — усі пункти «Завершення кожного промту» з CLAUDE.md.
```

**Перевір сам:**
- [ ] Рішення відкривається у Visual Studio / Rider, збирається без помилок.
- [ ] Запуск (F5) показує вікно «BrewLog»; з'явився файл `%LOCALAPPDATA%\BrewLog\brewlog.db`.
- [ ] `dotnet run --project src/BrewLog.UI -- --smoke` завершується, у `%TEMP%\brewlog-smoke.log` — успіх.

## B2 · Тема «як Дія» й оболонка з меню

```text
Промт B2 — тема інтерфейсу в стилі застосунку «Дія» з нашою палітрою «Темне пиво» і оболонка з бічним меню. Прочитай HANDOFF §9.1–9.3 і §4.4.

1. Themes (усе підключити в App.xaml через MergedDictionaries):
   - Colors.xaml — УСІ ключі з HANDOFF §9.2 як Color і SolidColorBrush; градієнти фону й DocCard як LinearGradientBrush (діагональ).
   - Typography.xaml — FontFamily (Segoe UI Variable Text / Display з резервом Segoe UI), іконковий шрифт (Segoe Fluent Icons з резервом Segoe MDL2 Assets), стилі TextBlock: PageTitle (30, SemiBold), PageSubtitle (15, Text.Secondary), SectionTitle (20, SemiBold), Body (15), Caption (13, Text.Secondary), BigNumber (34, SemiBold).
   - Controls.xaml — повністю власні ControlTemplate зі станами Hover / Pressed / Disabled / Focus:
     Card (біла, радіус 24, тінь DropShadowEffect дуже м'яка), DocCard (градієнт DocCard.Gradient, білий текст, радіус 24),
     PrimaryButton (темна «пігулка» Accent, висота 48, радіус 24, білий текст), SecondaryButton (біла з рамкою Border), TextButton, IconButton (кругла 40, гліф),
     InputBox (TextBox: висота 48, радіус 14, заливка Surface.Muted, без рамки, у фокусі рамка Accent, плейсхолдер через Tag), InputPassword, InputCombo (ComboBox з власним шаблоном і випадним списком-карткою), DatePicker у тому ж стилі,
     CheckBox і RadioButton у стилі теми, SegmentedButton (RadioButton-сегменти для перемикачів «Дошка/Список», фільтрів типів),
     Chip (заокруглений, м'який тонований фон) + варіанти для 5 етапів і Success/Warning/Danger/Info,
     ListRowCard (рядок-картка зі стрілкою › справа), тонкий ScrollViewer, ToolTip, TabControl/TabItem (вкладки-«пігулки»).
   - DataGrid.xaml — CleanDataGrid: без ліній сітки, рядок 44, заголовки стилем Caption, виділення Accent.Soft, hover Surface.Muted, контейнер-картка з радіусом.
   - Стиль вікна BrewWindow: фон-градієнт, мінімум 1200×760, шрифт за замовчуванням.
2. Shell: MainWindow + MainViewModel + MenuItemViewModel.
   - Зліва — біла заокруглена панель-меню шириною ~260: зверху логотип (текст «BrewLog» + проста іконка-келих через Path або гліф), пункти (іконка + підпис), активний — темна «пігулка» з білим текстом, hover — Surface.Muted. Унизу — блок профілю (коло з ініціалами, ім'я, роль) і кнопка «Вийти» (поки без дії для заглушки сесії).
   - Справа — ContentControl із CurrentPage, відступи 32.
   - Меню будується з ICurrentSession.Sections. Підписи й іконки розділів — в одному місці (SectionCatalog): Рецепти, Партії, Мої партії, Журнал вимірів, Розлив, Склад, Звіти, Користувачі.
   - AppSection.Users не навігує, а відкриває діалог через WindowService (поки DialogService.ShowInfo «Буде в B7»).
3. Services (UI): NavigationService (Navigate(AppSection) → ViewModel сторінки з фабрики-словника в AppServices; якщо сторінки ще немає — PlaceholderPage), WindowService (каркас із коментарем, куди додавати методи), DialogService + Features/Common/MessageDialog (Window у стилі теми: іконка за типом, заголовок, текст, 1–2 кнопки; ShowInfo, ShowError, Confirm → bool).
4. Features/Common/PlaceholderPage — велика картка «Розділ у розробці» з назвою розділу та іконкою.
5. DataTemplates ViewModel → View в App.xaml.
6. SmokeRunner: MainWindow, MessageDialog, PlaceholderPage для кожного AppSection.

Якість дизайну важлива: має виглядати сучасно, чисто й «дорого», як державний застосунок «Дія», але з нашими кольорами. Не копіюй логотипи, іконки чи графіку «Дії». Жодних кольорів і розмірів «у лоб» поза Themes.
```

**Перевір сам:**
- [ ] Фон вікна — м'який кремово-бежевий градієнт, меню — біла заокруглена панель.
- [ ] Клік по пунктах перемикає сторінки-заглушки, активний пункт — темна «пігулка».
- [ ] «Користувачі» показує стилізоване повідомлення, а не системний MessageBox.
- [ ] Якщо щось виглядає «не так» — зроби скріншот і попроси агента виправити саме це.

---

# Етап 2. Рецепти: логіка

## B3 · Домен рецептів і всі формули

```text
Промт B3 — домен рецептів і всі розрахунки. Прочитай HANDOFF §5.1 (ієрархії Ingredient та IbuFormula), §5.2–5.4 і §6.1 (формули з еталонами).

1. Domain/Recipes:
   - IngredientKind, HopUse, IngredientData (record з усіма полями, nullable — для БД, BeerXML і редактора).
   - Ingredient (abstract) + Malt, Hop, Yeast, Adjunct: властивості з HANDOFF §5.1; ApplyTo(RecipeStats), DescribeContribution(RecipeStats), ToData(), Clone(), Validate() → список помилок українською (кількість > 0, yield 0..100, alpha 0..25, зброджування 50..100, температура min < max тощо).
   - IngredientFactory: Create(IngredientKind) — новий порожній інгредієнт; FromData(IngredientData). Єдине місце з switch за типом.
   - RecipeStats і RecipeCalculator — точно за HANDOFF §6.1 (константа 385,6; FG від максимального зброджування, без дріжджів 0,75; IBU рахує обрана IbuFormula з G = OG). RecipeStats зберігає внесок кожного рядка (пункти, SRM-частку, IBU), щоб DescribeContribution показував «+0,031 OG · 4,2 SRM» / «18,4 IBU» / «зброджування 77 %».
   - Recipe (IEntity): поля таблиці recipes; Ingredients як IReadOnlyList; AddIngredient, RemoveIngredient, MoveIngredient; IngredientsOf<T>() where T : Ingredient; ScaleFactor(volumeL); Validate().
   - BeerStyle (IEntity) з ValueRange для OG, FG, ABV, IBU, SRM; StyleCheck.Check(style, stats) → список StyleParameterCheck (назва параметра, значення, діапазон, статус Нижче/ВМежах/Вище, позиція для шкали).
   - SrmColor.ToRgb(srm) → (byte R, byte G, byte B) за формулою HANDOFF §5.4, без WPF-типів.
2. Domain/Ibu: IbuFormula (abstract: Code, DisplayName, Calculate(hop, boilGravity, volumeL)), TinsethFormula, RagerFormula, IbuFormulaFactory (FromCode, All).
3. Тести (папки Recipes/ та Ibu/). Очікування — лише літерали з HANDOFF §6.1; допуски: 0,001 для густини, 0,2 для IBU, 0,05 для кольору.
   - Екстракт і густина: OG = 1,0483 (5 кг Pilsner); цукор +8,38 пункта й екстракт (IsMashed = false) без ефективності, цукор без кольору ([Theory]); FG = 1,0125 і ABV = 4,92; FG бере максимальне зброджування серед дріжджів, без дріжджів — 0,75 ([Theory]).
   - Колір і межі: SRM ≈ 7,14 і EBC ≈ 14,06; порожній рецепт і обсяг 0 не дають NaN чи Infinity ([Theory]); SrmColor: SRM 0 — білий, SRM 10 ≈ (198, 71, 7) з допуском ±1.
   - IBU: Tinseth ≈ 30,1 при 60 хв і 0 при 0 хв ([Theory]); Rager ≈ 40,2 при G = 1,050 і ≈ 36,5 при G = 1,070 ([Theory]); сухе охмелення = 0 для обох формул ([Theory]); IbuFormulaFactory: невідомий код — DomainException.
   - Інгредієнти: Validate ловить значення поза межами для кожного типу ([Theory]); IngredientFactory.FromData створює потрібний клас ([Theory]); ToData → FromData зберігає поля, а Clone незалежний від оригіналу.
   - Рецепт: Add, Remove, Move і IngredientsOf<T> працюють із правильними рядками; ScaleFactor(50) для 23 л ≈ 2,174; Validate відхиляє обсяг 0; StyleCheck дає Нижче / ВМежах / Вище і позицію 0..1 ([Theory]).

Жодних switch/if за типом інгредієнта чи формулою поза фабриками — лише override. XML-коментарі українською на кожному публічному класі й методі розрахунку (з формулою в коментарі).
```

**Перевір сам:**
- [ ] `dotnet test` — усі зелені, сценарії з промту покриті.
- [ ] Відкрий `Malt.cs` і `Hop.cs` — переконайся, що розумієш, як кожен інгредієнт «сам додає внесок» (це питання на захисті).

## B4 · Дані рецептів, довідники й демо-рецепти

```text
Промт B4 — дані рецептів і довідників, сервіси та демо-сідери. Прочитай HANDOFF §7, §8 (RecipeService, сідери) і §10.

1. Data/Rows: StyleRow, IngredientCatalogRow, RecipeRow, RecipeIngredientRow. Мапінг рядок ↔ IngredientData ↔ Ingredient — лише через IngredientFactory і Ingredient.ToData().
2. Інтерфейси в Domain/Abstractions: IStyleRepository, IIngredientCatalogRepository, IRecipeRepository.
   Реалізації в Data/Repositories, усі наслідують Repository<T>:
   - StyleRepository; IngredientCatalogRepository (каталог як Ingredient з Amount = 0: GetByKind, FindByName(kind, name) без регістру, IsUsed(id));
   - RecipeRepository: GetById вантажить рядки за sort_order; Insert/Update — транзакція: рецепт + повний перезапис рядків; Delete — каскадом; IsUsedInBatches(id).
3. Domain/Services:
   - CatalogService: GetStyles, SaveStyle, DeleteStyle (не можна, якщо використовується), GetCatalog(kind?), SaveCatalogItem, DeleteCatalogItem (не можна, якщо використовується в рецептах або на складі). Перевірка Permission.EditReferences.
   - RecipeService — контракт HANDOFF §8: GetAll і Get (Permission.ViewRecipes), Calculate, Save (Permission.EditRecipes + Validate + updated_at), Delete (Permission.EditRecipes; рецепт із партіями не видаляється — DomainException «Рецепт використовується в партіях», перевірка через IsUsedInBatches), Duplicate («<назва> (копія)»), EstimateCostPerLiter (поки за last_price каталогу; в A1 перейде на ціни складу).
4. Сідери:
   - CatalogSeeder (Order 20): 15 стилів BJCP 2021 зі списку HANDOFF §10 з реальними діапазонами з настанов BJCP 2021; ~25 інгредієнтів з HANDOFF §10 з реалістичними характеристиками (yield, °L, alpha, зброджування, температури), мінімумами й цінами в грн; 2 вигадані постачальники.
   - RecipeSeeder (Order 30): 5 рецептів із HANDOFF §10 (American IPA — із сухим охмеленням, American Pale Ale — з формулою Rager). Показники КОЖНОГО мають потрапляти в діапазони свого стилю: порахуй через RecipeCalculator і StyleCheck, за потреби підкоригуй кількості.
5. Зареєструй усе в AppServices і в списку сідерів.
6. У SmokeRunner виведи в лог для кожного демо-рецепта OG / FG / ABV / IBU / SRM і результат StyleCheck — це залишається як корисна діагностика.

7. Тести (Services/ і Data/; заглушки репозиторіїв — власні класи у Fakes/):
   - RecipeService: Save без дозволу й з невалідним рецептом — DomainException і нічого не збережено ([Theory]); GetAll для помічника — DomainException; Duplicate дає нову назву й незалежні рядки; Delete рецепта з партіями — DomainException.
   - CatalogService: видалення стилю чи інгредієнта, які використовуються, — DomainException ([Theory]).
   - SQLite: RecipeRepository зберігає й читає рецепт з усіма типами рядків у правильному порядку; кожен із 5 демо-рецептів потрапляє в діапазони свого стилю ([Theory]).

У журналі стану: «⚠ видаліть brewlog.db».
```

**Перевір сам:**
- [ ] Видали `brewlog.db`, запусти застосунок — БД створилась без помилок.
- [ ] У `brewlog-smoke.log` усі 5 рецептів «в межах стилю».
- [ ] (Необов'язково) відкрий БД у DB Browser for SQLite — є стилі, інгредієнти, рецепти.

---

# Етап 3. Користувачі

## B5 · Користувачі, ролі й автентифікація

```text
Промт B5 — ієрархія користувачів і автентифікація. Прочитай HANDOFF §3 (ролі, дозволи, демо-логіни), §5.1 (ієрархія User), §7 (users), §8 (ICurrentSession).

1. Domain/Users — доповни мінімальний User із B1 (не ламаючи місць, де він уже використовується):
   - User (abstract): Id, Login, FullName, PasswordHash, IsActive, RememberTokenHash; abstract Role, RoleDisplayName, Permissions (IReadOnlySet<Permission>), Sections (IReadOnlyList<AppSection>); virtual HomeSection; Can(Permission).
   - Brewer, Assistant, Owner — набори дозволів і розділів ТОЧНО з HANDOFF §3.1–3.2 (порядок розділів важливий).
   - UserRole enum; UserFactory.Create(role) — єдине місце зі switch за роллю.
2. Data: UserRow, UserRepository : Repository<User> (мапінг через UserFactory; FindByLogin без регістру), Security/BCryptPasswordHasher : IPasswordHasher (BCrypt.Net-Next).
3. Domain/Services:
   - AuthService: Login(login, password) → User або DomainException («Невірний логін або пароль» / «Обліковий запис вимкнено»); IssueRememberToken(user) → випадковий токен (32 байти, base64), у БД — лише його хеш; LoginWithToken(userId, token); RevokeRememberToken(user).
   - UserService (Permission.ManageUsers): GetAll, Create(login, fullName, role, password), Update(id, fullName, role), ResetPassword(id, newPassword), SetActive(id, bool). Правила: логін унікальний, 3–32 символи латиницею/цифрами; пароль ≥ 6 символів; не можна вимкнути або понизити себе; має лишатися ≥ 1 активний власник.
   - UserService.GetAssistants() (Permission.ManageBatches): активні помічники для вибору виконавця партії. Пивовар не має ManageUsers, тому GetAll йому не підходить.
4. UI/Services/CurrentSession : ICurrentSession — User, IsAuthenticated, Can, Sections, Changed, SignIn(user), SignOut(). У AppServices заміни AllowAllSession на CurrentSession (AllowAllSession прибери або залиш лише для внутрішніх потреб SmokeRunner).
5. UserSeeder (Order 10): 3 користувачі з HANDOFF §3.3 (паролі — лише хеші).
6. Тести (Users/ і Services/; без БД, із заглушками у Fakes/):
   - Ролі: матриця дозволів ТОЧНО з HANDOFF §3.2 ([Theory]: роль, дозвіл, очікування — усі 33 комбінації); розділи в порядку §3.1 і HomeSection ([Theory]); UserFactory створює потрібний клас ([Theory]).
   - AuthService: невірний логін і невірний пароль дають однакове повідомлення; вимкнений обліковий запис не входить; токен «Запам'ятати мене» працює, у сховищі лише його хеш, після RevokeRememberToken вхід за токеном заборонено.
   - UserService: некоректні логін чи пароль і дублікат логіна — DomainException ([Theory]); не можна вимкнути чи понизити себе; має лишатися ≥ 1 активний власник; без ManageUsers — DomainException; GetAssistants доступний пивоварові й повертає лише активних помічників.

У журналі стану — «⚠ видаліть brewlog.db».
```

**Перевір сам:**
- [ ] Видали `brewlog.db`, запусти — у таблиці `users` три записи з хешами `$2a$...`, а не паролями.
- [ ] Відкрий `Brewer.cs` / `Owner.cs` — розумієш, як роль сама визначає меню й дозволи (питання на захисті).

## B6 · Вікно входу, «Запам'ятати мене», меню за роллю

```text
Промт B6 — вікно входу, «Запам'ятати мене» і меню за роллю. Прочитай HANDOFF §3.1, §4.4, §9.1, §9.3 (рядок 1 і «Оболонка»).

1. Features/Login: LoginWindow + LoginViewModel.
   - Дизайн: градієнтний фон; по центру біла Card ~420 px: логотип/«BrewLog» великим, підзаголовок «Облік виробництва броварні», InputBox «Логін», InputPassword «Пароль» (значення в VM — через PasswordChanged у code-behind), CheckBox «Запам'ятати мене», PrimaryButton «Увійти» на всю ширину, текст помилки кольором Danger під кнопкою, Enter = увійти, фокус одразу в полі логіна.
   - Під карткою дрібним Caption — підказка з демо-логінами, лише в Debug (#if DEBUG).
2. UI/Services/RememberMeStore: %LOCALAPPDATA%\BrewLog\session.json {userId, token}; Save, Load, Clear.
3. Запуск (App.OnStartup, файл спільний — лише доповнюй): ініціалізація БД → якщо є валідний збережений токен — одразу MainWindow; інакше LoginWindow → після успішного входу MainWindow. ShutdownMode такий, щоб застосунок не закривався при переході між вікнами.
4. Оболонка (доповни те, що зроблено в B2, не переписуй): меню з CurrentSession.Sections у порядку HANDOFF §3.1; стартова сторінка — HomeSection; блок профілю внизу (ініціали в колі, ім'я, роль); «Вийти» → RevokeRememberToken + Clear + SignOut → закрити MainWindow → показати LoginWindow. Оболонка реагує на Changed.
5. SmokeRunner: LoginWindow; далі для кожної ролі — SignIn напряму, створити MainWindow і всі сторінки з її Sections.
```

**Перевір сам:**
- [ ] `brewer` бачить: Рецепти, Партії, Склад; `assistant`: Мої партії, Журнал вимірів, Розлив; `owner`: Партії, Склад, Звіти, Користувачі.
- [ ] Невірний пароль — червоний текст помилки, без падіння.
- [ ] «Запам'ятати мене» → перезапуск відкриває одразу головне вікно; «Вийти» → знову вхід, і після перезапуску теж вхід.

## B7 · Діалог «Користувачі»

```text
Промт B7 — керування користувачами (для власника). Прочитай HANDOFF §3, §9.3 (рядок «Користувачі»).

Features/Users: UsersDialog (Window) + UsersViewModel. Відкривається пунктом меню «Користувачі» — заміни заглушку з B2 у MainViewModel/WindowService (WindowService.ShowUsers()).
- Ліворуч — список рядків-карток (ListRowCard): коло з ініціалами, ім'я, логін, Chip ролі; вимкнені — приглушені з позначкою «вимкнено». Зверху — пошук і «+ Новий користувач».
- Праворуч — Card з формою вибраного: ім'я, логін (редагується лише для нового), роль (InputCombo), «Активний» (перемикач), для нового — пароль і підтвердження; кнопки «Зберегти» (Primary), «Скинути пароль» (Secondary → маленький діалог нового пароля з підтвердженням).
- Усі правила — в UserService (HANDOFF §3, B5); помилки — через DialogService.
- SmokeRunner — діалог.
```

**Перевір сам:**
- [ ] Створи нового помічника, увійди ним — бачить меню помічника.
- [ ] Спроба вимкнути себе або останнього власника — зрозуміле повідомлення.

---

# Етап 4. Рецепти: екрани

## B8 · Сторінка «Рецепти»

```text
Промт B8 — сторінка «Рецепти». Прочитай HANDOFF §9.1, §9.3 (рядок 2), §3.2. Сесія й дозволи вже справжні (B5–B7).

Features/Recipes: RecipesPage (UserControl) + RecipesViewModel; зареєструй для AppSection.Recipes.
- Шапка: PageTitle «Рецепти», PageSubtitle «N рецептів»; праворуч кнопки (лише з Permission.EditRecipes): «Новий рецепт» (Primary), «Імпорт BeerXML», «Експорт BeerXML», «Довідники» (EditReferences) — Secondary.
- Рядок фільтрів: InputBox пошуку за назвою (миттєвий), InputCombo фільтра за стилем («Усі стилі» + стилі, що є в рецептах).
- Ліворуч CleanDataGrid у картці: назва, стиль (код + назва), OG, FG, ABV, IBU, колір (кругла плашка кольору пива через SrmColor + EBC), грн/л. Сортування за колонками. Подвійний клік — редагувати.
- Праворуч (ширина ~380) панель прев'ю вибраного: DocCard (назва великим, стиль, ABV дуже великим, OG → FG, IBU, EBC, грн/л), під нею Card зі складом за групами «Солод / Хміль / Дріжджі / Додатки» (назва — кількість одиниця), нотатки; кнопки «Редагувати», «Дублювати», «Видалити» (Confirm) — лише з EditRecipes.
- Порожній стан: картка «Нічого не знайдено» / «Рецептів ще немає».
- Розрахунки — лише RecipeService.Calculate і EstimateCostPerLiter; ViewModel рядка готує відформатовані значення (формати — CLAUDE.md).
- Converters/SrmToBrushConverter.
- Імпорт/Експорт поки показують DialogService.ShowInfo «Буде в B10», «Довідники» — «Буде в B11»; WindowService.ShowRecipeEditor(int? id) → bool поки показує «Буде в B9».
- SmokeRunner — сторінка.
```

**Перевір сам:**
- [ ] Увійди як `brewer` — бачиш 5 рецептів, пошук і фільтр працюють, прев'ю оновлюється.
- [ ] `owner` і `assistant` не мають пункту «Рецепти».
- [ ] Кольорові плашки схожі на колір пива (Pils — світлий, Stout — майже чорний).

## B9 · Редактор рецепта з живим розрахунком

```text
Промт B9 — вікно «Редактор рецепта». Прочитай HANDOFF §9.3 (рядок 3), §5.1, §6.1.

Features/RecipeEditor: RecipeEditorWindow + RecipeEditorViewModel + IngredientLineViewModel (обгортка над Ingredient; поліморфні колонки через DataTemplate за типом там, де доречно).
- Розкладка в три колонки на градієнтному фоні:
  1) Card «Параметри»: назва, стиль (InputCombo), обсяг партії (л), ефективність (%), час кипіння (хв), формула IBU (SegmentedButton Tinseth / Rager з IbuFormulaFactory.All), нотатки.
  2) Чотири Card-секції: «Солод», «Хміль», «Дріжджі», «Додатки». У кожній — редагована таблиця (CleanDataGrid) і кнопка «+ Додати» з вибором позиції каталогу цього типу (поп-ап/комбо з пошуком) → IngredientFactory.FromData копіює характеристики.
     Солод: назва, кг, yield %, °L, внесок. Хміль: назва, г, alpha %, хв, використання (Кипіння / Сухе охмелення), внесок IBU. Дріжджі: назва, пак., зброджування %, діапазон t°. Додатки: назва, кг, yield %, внесок. Іконка-кошик для видалення рядка.
     Колонка «Внесок» — Ingredient.DescribeContribution з поточного RecipeStats.
  3) Права колонка (не прокручується разом зі списками): DocCard з живими OG, FG, ABV, IBU, SRM/EBC (плашка кольору), собівартість грн/л; під нею Card «Відповідність стилю» — для 5 параметрів Controls/StyleRangeBar (смуга діапазону стилю, маркер значення, підпис «1,052 · 1,044–1,060»; у межах — Success, поза — Danger).
- Перерахунок — на КОЖНУ зміну будь-якого поля чи рядка (без кнопки «Порахувати»), лише через RecipeService.Calculate.
- «Зберегти» (Ctrl+S): Validate → помилки під полями або через DialogService; після збереження вікно закривається з результатом true. «Скасувати»/Esc: якщо є незбережені зміни — Confirm.
- WindowService.ShowRecipeEditor(int? id) → bool; сторінка «Рецепти» після true перечитує дані й лишає вибраним збережений рецепт.
- SmokeRunner — вікно (новий рецепт і редагування демо-рецепта).
```

**Перевір сам:**
- [ ] Змінюєш кількість солоду — OG, ABV і колір змінюються одразу.
- [ ] Хміль «Сухе охмелення» не додає IBU; Tinseth ↔ Rager змінює IBU.
- [ ] Шкали стилю стають червоними, коли параметр виходить за межі.
- [ ] Новий рецепт зберігається й з'являється в таблиці; «Скасувати» з незбереженими змінами питає підтвердження.

## B10 · Імпорт і експорт BeerXML

```text
Промт B10 — обмін рецептами у форматі BeerXML 1.0. Прочитай HANDOFF §5.3 (Factory).

1. Data/BeerXml/BeerXmlSerializer — власний код на System.Xml.Linq, без бібліотек, числа в InvariantCulture:
   - Import(Stream) → IReadOnlyList<Recipe> (у файлі може бути кілька RECIPE всередині RECIPES).
   - Export(IEnumerable<Recipe>, Stream) — з VERSION=1 у кожному записі.
   - Мапінг: RECIPE/NAME, TYPE (All Grain), BATCH_SIZE (л), BOIL_TIME, EFFICIENCY, STYLE (NAME, CATEGORY_NUMBER + STYLE_LETTER → пошук стилю за кодом, інакше без стилю);
     FERMENTABLES/FERMENTABLE: TYPE Grain → Malt (IsMashed = true), Extract / Dry Extract → Malt (IsMashed = false), Sugar / Adjunct → Adjunct; AMOUNT (кг), YIELD (%), COLOR (°L);
     HOPS/HOP: AMOUNT у кг → г (×1000), ALPHA (%), TIME (хв), USE «Dry Hop» → DryHop, решта → Boil;
     YEASTS/YEAST: ATTENUATION (%), MIN_TEMPERATURE / MAX_TEMPERATURE (°C), кількість — 1 пак., якщо не вказано.
     MISCS, WATERS, MASH — ігнорувати при імпорті, при експорті — порожні обов'язкові секції.
   - Інгредієнти створюються ЛИШЕ через IngredientFactory.FromData.
2. RecipeService: Import(path) — для кожного рядка знайти позицію каталогу за kind + name без регістру; якщо немає — створити позицію каталогу з характеристиками з файлу (мінімум 0, ціна 0); зберегти рецепти; повернути підсумок (скільки рецептів, скільки нових позицій). Export(recipeIds, path).
3. Сторінка «Рецепти»: Імпорт — OpenFileDialog (*.xml), Експорт вибраного рецепта — SaveFileDialog; результат через DialogService («Імпортовано 2 рецепти, додано 3 нові інгредієнти в довідник»). Некоректний файл — зрозуміла помилка, а не виняток.
4. Тести (BeerXml/BeerXmlTests.cs):
   - експорт рецепта з усіма типами інгредієнтів у MemoryStream → імпорт → така сама кількість рядків кожного типу й OG у межах 0,001. Запускати під культурою uk-UA: десяткова кома не має потрапити у файл;
   - файл із кількома RECIPE дає кілька рецептів;
   - хміль: AMOUNT у кг стає грамами, USE «Dry Hop» стає DryHop;
   - FERMENTABLE TYPE: Grain, Extract і Sugar дають правильний клас та IsMashed ([Theory]);
   - некоректний XML дає DomainException, а не XmlException.
5. Поклади приклад docs/samples/american-ipa.beerxml.xml (експорт демо-рецепта) — для демонстрації на захисті.
```

**Перевір сам:**
- [ ] Експортуй рецепт, видали його (партій ще немає, тому видалення спрацює), імпортуй файл назад — показники ті самі.
- [ ] Імпортуй `docs/samples/...` з новою назвою хмелю (відредагуй файл у блокноті) — позиція з'являється в довіднику.

## B11 · Довідники стилів та інгредієнтів

```text
Промт B11 — діалог «Довідники». Прочитай HANDOFF §1 (довідники редагує користувач), §3.2 (EditReferences).

Features/References: ReferencesDialog (Window) + ReferencesViewModel, відкривається з кнопки «Довідники» на сторінці «Рецепти» (лише EditReferences).
- TabControl (вкладки-«пігулки» з теми): «Стилі BJCP» і «Інгредієнти».
- «Стилі BJCP»: список-картки ліворуч (код, назва, категорія) з пошуком; праворуч форма: код, назва, категорія, 5 діапазонів (OG, FG, ABV, IBU, SRM — min і max поруч); валідація min ≤ max і розумних меж; «Новий», «Зберегти», «Видалити» (не можна, якщо стиль використовується в рецептах — повідомлення через DialogService).
- «Інгредієнти»: SegmentedButton за типом + пошук; список; форма характеристик підбирається через окремий DataTemplate для кожного типу (Malt / Hop / Yeast / Adjunct) — без switch у коді: солод — yield, °L, «зерно/екстракт»; хміль — alpha; дріжджі — зброджування, t° min/max; додаток — yield. Ціни й мінімуми тут НЕ редагуються (це власник у «Складі») — показати їх лише для читання.
- Усі зміни — через CatalogService. Після закриття діалогу сторінка «Рецепти» перераховує показники.
- SmokeRunner — діалог.
```

**Перевір сам:**
- [ ] Змінюєш діапазон IBU стилю American IPA — шкала в редакторі рецепта змінюється.
- [ ] Змінюєш alpha хмелю в довіднику — нові рецепти беруть нове значення.
- [ ] Видалити використовуваний стиль не дає.

---

# Етап 5. Передача

## B12 · Полірування першої половини й передача

```text
Промт B12 — полірування моєї частини (B) і передача проєкту розробнику A.

1. Пройдись по своїх екранах (Вхід, оболонка, MessageDialog, Користувачі, Рецепти, Редактор, Довідники): однакові відступи 32/24/16, заголовки, порожні стани, ToolTip на кожній іконковій кнопці, гарячі клавіші (Ctrl+S — зберегти в редакторі, Ctrl+F — пошук, Esc — закрити діалог, Enter — основна дія діалогу), Tab-порядок, формати чисел із CLAUDE.md, вирівнювання чисел у таблицях праворуч.
2. Для кожної ролі перевір по коду: кнопки без дозволу приховані, сервіси частини B перевіряють дозволи.
3. Прибери попередження компілятора у файлах B, мертвий код, невикористані using; перевір XML-коментарі українською на всіх публічних класах B.
4. Аудит своїх тестів: сценарії з промтів є; очікування — літерали, а не та сама формула, що в коді; дублікатів немає. Знайдений дефект виправляй у коді, а не підганяй тест.
5. README.md — заповни свої розділи (розділи A не чіпай):
   - «Рецепти й розрахунки» — коротко формули (OG, FG, ABV, IBU Tinseth/Rager, SRM Morey, собівартість) і як інгредієнт сам додає внесок;
   - «Користувачі й ролі» — як роль сама визначає дозволи й меню, BCrypt, «Запам'ятати мене»;
   - «BeerXML»;
   - таблиця «Класи частини B для ЛР 1»: клас — роль (ієрархія / патерн / узагальнений / сервіс) — файл;
   - «Сценарій демонстрації — частина B»: 6–8 кроків (вхід пивоваром → рецепт → редактор → BeerXML → довідники → вхід власником → користувачі).
6. Видали локальну БД, переконайся, що все засівається з нуля, smoke зелений.
7. HANDOFF §13: рядок «Передача A»: що готово, які контракти §8 уже працюють, які розділи меню ще заглушки, що A має знати перед стартом.
```

**Перевір сам:**
- [ ] Пройди свій сценарій демонстрації з README від початку до кінця.
- [ ] Заповни «Мої висновки» в RETRO.

→ **B-E**, merge PR, напиши A, що можна починати.

---

## Рятувальні промти (коли щось пішло не так)

**Не збирається:**
```text
dotnet build падає. Знайди причину (повний текст помилок), виправ мінімально, не переписуй робочий код і не змінюй контракти HANDOFF §8. Поясни в 2–3 рядках, що було не так.
```

**Застосунок падає або вікно не відкривається (помилка XAML):**
```text
Під час запуску <що робив> застосунок падає. Ось текст помилки / лог з %LOCALAPPDATA%\BrewLog\logs: <встав>. Знайди причину, виправ, додай цей випадок у SmokeRunner, якщо його там немає, і перевір smoke.
```

**Виглядає не так:**
```text
На скріншоті <екран>. Проблеми: <перелік: з'їхало, не видно тексту, не той колір...>. Виправ через стилі в Themes, без кольорів «у лоб» у розмітці.
```

**Агент зробив не те:**
```text
Останній результат не відповідає задачі: <що не так>. Спершу покажи git diff --stat і запропонуй, що відкотити. Нічого не видаляй без мого підтвердження. Потім зроби заново з урахуванням: <уточнення>.
```

**Конфлікт при злитті з main:**
```text
git merge main дає конфлікти в <файли>. Розв'яжи їх, зберігши зміни ОБОХ розробників (спільні файли — лише доповнюємо). Після цього build, test, smoke і коротко поясни, що об'єднав.
```
