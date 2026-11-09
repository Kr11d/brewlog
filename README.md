# BrewLog

Настільний застосунок для обліку виробництва малої броварні: рецепти з живим розрахунком показників, партії з етапами, журнал бродіння, склад сировини, розлив із PDF-етикетками, звіти й обмін рецептами у форматі BeerXML.

Командний навчальний проєкт із дисципліни «Парадигма ООП» (лабораторні роботи № 1–3). Стек: C#, .NET 10, WPF, SQLite. Статус: у розробці.

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

## Сценарій демонстрації — частина B

1. Увійти як `brewer` / `brewer123`: у меню Рецепти, Партії, Склад.
2. На сторінці «Рецепти» відфільтрувати таблицю пошуком і стилем, вибрати American IPA й показати прев'ю зі складом і собівартістю.
3. Натиснути «Редагувати»: змінити кількість солоду — OG, ABV і колір оновлюються одразу; перемкнути Tinseth на Rager — змінюється IBU; вивести показник за межі стилю — шкала стає червоною. Закрити без збереження.
4. Натиснути «Імпорт BeerXML» і вибрати `docs/samples/american-ipa.beerxml.xml`: з'являється новий рецепт. Вибрати його й натиснути «Експорт BeerXML».
5. Відкрити «Довідники»: змінити діапазон IBU стилю American IPA й показати нову шкалу в редакторі; спробувати видалити цей стиль — застосунок відмовляє, бо стиль використовується в рецептах. На вкладці «Інгредієнти» показати, що форма в кожного типу своя.
6. Вийти й увійти як `owner` / `owner123`: меню інше, рецептів немає.
7. Відкрити «Користувачі»: створити помічника; спробувати вимкнути себе — застосунок відмовляє.
8. Вийти й увійти новим помічником: у меню Мої партії, Журнал вимірів, Розлив.

## Документація

- [Опис проєкту](docs/BrewLog-опис.pdf)
- [План і архітектура](HANDOFF.md)
- [Ретроспектива](docs/RETRO.md)

## Використання ШІ

Під час розробки використовується Claude Code (Anthropic) як асистент. Історія запитів зберігається; весь код переглядається й розуміється авторами. Робочі домовленості з асистентом — у [`CLAUDE.md`](CLAUDE.md).
