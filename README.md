# CrossApp

### Інваріанти доменної моделі (Лабораторна 4)
1. Ідентифікатори сутностей та ключові поля не можуть бути порожніми (`ArgumentException`).
2. Неможливо видати примірник, який вже перебуває на руках у читача (`InvalidOperationException`).
3. Дата повернення книги не може бути раніше дати її видачі (`ArgumentOutOfRangeException`).
4. Неможливо повторно закрити вже закриту видачу (`InvalidOperationException`).

### Сервісний шар і сховища (Лабораторна 5)
- **Інтерфейс**: `IBookStore`
- **Реалізації**:
  - `InMemoryBookStore` — сховище в оперативній пам'яті на базі словника.
  - `FileBookStore` — файлове сховище (JSON) з кешуванням у пам'яті.
  - `CachingBookStore` — декоратор над сховищем для локального кешування.
- **Сервіс**: `LendingService` — залежить виключно від абстракції `IBookStore` (ручний DI через первинний конструктор).
- **Фабрика**: `StoreFactory` — створює та конфігурує сховище на основі аргументів CLI (`--file`).

### Доменні події та делегати (Лабораторна 6)
- **Події домену**:
  - `BookChangeKind`: `BookAdded`, `CopyIssued`, `CopyReturned`.
  - `BookChangedEventArgs`: клас даних події (`CopyId`, `Kind`, `ReaderId`, `DueDate`, `At`).
- **Публікація подій**: `LendingService.Changed` (`event EventHandler<BookChangedEventArgs>?`), подія піднімається безпечно через `?.Invoke(...)` виключно після успішної зміни стану (якщо інваріант викинув виняток — подія не генерується).
- **Підписники у Cli**:
  - Локальний метод `OnLibraryChanged`: виводить подію у консоль та записує у лог-файл `logs/app.log`.
  - Лямбда-вираз із замиканням: рахує загальну кількість подій (`changes++`).
  - Продемонстровано відписку (`service.Changed -= OnLibraryChanged`), після чого локальний метод перестає реагувати, а лічильник продовжує фіксувати події.
- **Делегат Func**: метод `service.FindAll(Func<BookCopy, bool> predicate)` для фільтрації сутностей без прив'язки до внутрішнього сховища.

#### Додаткові завдання (Лабораторна 6)
1. **Інтерфейс `INotificationSink`**:
   - Контракт `INotificationSink` з методом `Notify(string message)` у `Core/Abstractions`.
   - Реалізації `ConsoleSink` та `FileSink` у `Cli/Notifications`.
   - Передається в `LendingService` через конструктор (ручний DI).
   - *Порівняння з подіями*: Події реалізують патерн Observer з динамічною підпискою (`+=`/`-=` кількох підписників без знання сервісу про них). Приймач `INotificationSink` є явною залежністю сервісу, що передається під час створення (жорсткіший зв'язок, фіксований адресат, складно динамічно масштабувати на багатьох слухачів без Composite-обгортки).
2. **`Action<string>` у методі імпорту**:
   - `BookCsvImporter.Load(string path, Action<string>? onSkippedLine = null)` приймає делегат зворотного виклику для негайного звітування про пропущені рядки (порожні, коментарі `#`, заголовок) або помилкові записи.

### Звіти через LINQ (Лабораторна 7)
Уся логіка звітів зосереджена в `Core/Reports/ReportService`: методи приймають `IEnumerable<T>` і повертають `IReadOnlyList<DTO>` (record-и `BookLine`, `ReaderLine`, `OverdueLine`, `GroupSummary`, `ReaderActivity`), а не доменні сутності. `Program.cs` лише викликає звіти й друкує результат — у ньому немає жодного `Where`/`GroupBy`. Дані для звітів беруться з `Core/SampleData` (18 видань у 5 жанрах, 84 примірники, 8 читачів, 17 видач).

| Метод | Питання, на яке відповідає | Оператори |
|---|---|---|
| `TopReaders(loans, n)` | Хто з читачів має найбільше видач? | `GroupBy`, `Select`→DTO, `TopBy` (`OrderByDescending`+`ThenBy`+`Take`) |
| `TopBooks(books, n)` | Яких видань найбільше примірників? | `TopBy`, `Select`→DTO |
| `ScarceBooks(books, threshold)` | Яких видань замало (менше N примірників)? | `Where`, `OrderBy`+`ThenBy`, `Select`→DTO |
| `Overdue(loans, today)` | Які видачі прострочені (`DueDate < сьогодні`)? | `Where`, `OrderBy`+`ThenBy`, `Select`→DTO |
| `ByGenre(books)` | Як видання розподіляються за жанрами? | `GroupBy` (`OrdinalIgnoreCase`), `Count`, `Sum`, `OrderByDescending`+`ThenBy` |
| `Totals(books)` | Скільки всього видань/примірників, середнє та максимум? | `ToList`, `Sum`, `Average`, `Max` + перевірка на порожнечу |

Фрагмент групування з проєкцією:
```csharp
public static IReadOnlyList<GroupSummary> ByGenre(IEnumerable<Book> source) =>
    source.GroupBy(b => b.Genre, StringComparer.OrdinalIgnoreCase)
          .Select(g => new GroupSummary(g.Key, g.Count(), g.Sum(b => b.Copies)))
          .OrderByDescending(s => s.TotalCopies).ThenBy(s => s.Key, StringComparer.Ordinal)
          .ToList();
```

Підкоманди (`dotnet run --project src/Cli -- <команда>`): `report-top`, `report-books`, `report-low`, `report-overdue`, `report-groups`, `report-sum`, `report-demo`, а також `report-readers`, `report-query`, `report-bench` (додаткові) і `events` (демо лабораторної 6). Прапорець `--empty` підставляє порожні джерела: усі звіти відпрацьовують без винятку. Прапорець `--file` не сприймається як назва звіту.

#### Відкладене виконання (`report-demo`)
`DeferredExecutionDemo.Run` будує запит `source.Where(b => b.Copies < 3)`, але в цей момент жоден елемент не перевіряється — створюється лише опис запиту. Запит виконується щоразу, коли його перебирають: під час `Count()` або `ToList()`. Тому перший `Count()` дає **3** (Фундація, 451° за Фаренгейтом, Лісова пісня), а після `source.Add(...)` другий `Count()` проходить список заново, бачить нове видання з 1 примірником і повертає **4**. `ToList()` фіксує результат у знімку: після `source.Clear()` у знімку лишається **4** елементи, а той самий запит дає **0**. У звітах `ReportService` кожен ланцюжок закінчується `ToList()`, тому запит виконується рівно один раз — у момент виклику методу звіту.

#### Додаткові завдання (Лабораторна 7)
1. **GroupJoin** — `ReportService.ReadersActivity(readers, loans)` з'єднує читачів із видачами за `ReaderId` і проєктує в `ReaderActivity` (усього видач і скільки на руках). На відміну від `Join`, `GroupJoin` залишає читача навіть без видач (`R-008` з нулями). Команда `report-readers`.
2. **Метод-розширення** — `EnumerableExtensions.TopBy<T>(this IEnumerable<T>, Func<T,int> key, Func<T,string> tieBreaker, int n)`: сортування за спаданням ключа, другий ключ `Ordinal`, `Take(n)`. Використовується у `TopReaders` і `TopBooks`. Другий ключ є обов'язковим параметром, щоб жоден топ-N не втратив стабільність.
3. **Синтаксис запитів** — `ReportService.ByGenreQuery` (команда `report-query` перевіряє `SequenceEqual` з `ByGenre` → `True`):
   ```csharp
   from book in source
   group book by book.Genre.ToUpperInvariant() into g
   let key = g.First().Genre
   let total = g.Sum(b => b.Copies)
   orderby total descending, key
   select new GroupSummary(key, g.Count(), total)
   ```
   *Порівняння*: синтаксис запитів добре читається, коли в центрі групування (`group … into g`, `let` для проміжних значень замість повторного `g.Sum`). Але `group by` не приймає `StringComparer`, тож для злиття «Поезія»/«поезія» довелося нормалізувати ключ і брати назву з першого елемента групи; `orderby` теж порівнює рядки за культурою, а не `Ordinal`. Методний ланцюжок коротший, читається зліва направо і дає повний контроль над компараторами, тому в основних звітах лишився саме він.
4. **Stopwatch на 100 000 записах** (`report-bench`, `SampleData.Generate(100_000)`, середнє з 5 запусків після прогріву, .NET 10, Apple Silicon):

   | Варіант | Рядків | Час, мс |
   |---|---|---|
   | Ланцюжок без `ToList` у середині | 15 046 | ~3.7 |
   | `ToList` після кожного кроку | 15 046 | ~3.5 |
   | `OrderBy` до `Where` (типова помилка) | 15 046 | ~28.5 |
   | `ByGenre` (синтаксис методів) | 5 | ~7.5 |
   | `ByGenreQuery` (синтаксис запитів) | 5 | ~11.3 |

   Проміжні `ToList` тут майже нічого не змінюють: `OrderBy` — буферизувальний оператор і все одно копіює всю послідовність у пам'ять, а список після `Where` має відомий розмір, тож наступні кроки виділяють буфери точного розміру (різниця в межах ~5 %). Вирішальним є порядок операторів: якщо сортувати до фільтра, `OrderBy` сортує всі 100 000 елементів замість ~15 000, і це в ~8 разів повільніше. Варіант із синтаксисом запитів повільніший через `ToUpperInvariant()` (новий рядок на кожен елемент) і `let`, які компілятор розгортає в додаткові `Select` з анонімними типами.
