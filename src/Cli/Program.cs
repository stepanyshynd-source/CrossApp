using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Cli.Notifications;
using Core;
using Core.Abstractions;
using Core.Domain;
using Core.Dto;
using Core.Events;
using Core.Import;
using Core.Reports;
using Core.Services;
using Core.Storage;

const int ScarceThreshold = 3;

bool empty = args.Contains("--empty");
IReadOnlyList<Book> books = empty ? [] : SampleData.Books();
IReadOnlyList<Loan> loans = empty ? [] : SampleData.Loans(DateTime.Today);
IReadOnlyList<ReaderDto> readers = empty ? [] : SampleData.Readers();

var command = args.FirstOrDefault(a => !a.StartsWith("--")) ?? "report-top";

switch (command)
{
    case "report-top":
        Console.WriteLine("Топ-5 читачів за кількістю видач:");
        PrintReaders(ReportService.TopReaders(loans, 5));
        break;
    case "report-books":
        Console.WriteLine("Топ-5 видань за кількістю примірників:");
        PrintBooks(ReportService.TopBooks(books, 5));
        break;
    case "report-low":
        Console.WriteLine($"Дефіцитні видання (менше {ScarceThreshold} примірників):");
        PrintBooks(ReportService.ScarceBooks(books, ScarceThreshold));
        break;
    case "report-overdue":
        Console.WriteLine($"Прострочені видачі на {DateTime.Today:dd.MM.yyyy}:");
        PrintOverdue(ReportService.Overdue(loans, DateTime.Today));
        break;
    case "report-groups":
        Console.WriteLine("Видання за жанрами:");
        PrintGroups(ReportService.ByGenre(books));
        break;
    case "report-sum":
        var t = ReportService.Totals(books);
        Console.WriteLine($" видань {t.Items}, примірників {t.Total}, середнє {t.Average:F2}, макс {t.Max}");
        break;
    case "report-demo":
        var demo = DeferredExecutionDemo.Run(books, ScarceThreshold);
        Console.WriteLine($" Дефіцитних до змін:        {demo.Before}");
        Console.WriteLine($" Дефіцитних після змін:     {demo.After}");
        Console.WriteLine($" У знімку лишилось:         {demo.Snapshot}");
        Console.WriteLine($" Запит після Clear():       {demo.QueryAfterClear}");
        break;
    case "report-readers":
        Console.WriteLine("Активність читачів (GroupJoin читачі × видачі):");
        PrintActivity(ReportService.ReadersActivity(readers, loans));
        break;
    case "report-query":
        var byQuery = ReportService.ByGenreQuery(books);
        Console.WriteLine("Видання за жанрами (синтаксис запитів):");
        PrintGroups(byQuery);
        Console.WriteLine($" Збігається з ByGenre: {byQuery.SequenceEqual(ReportService.ByGenre(books))}");
        break;
    case "report-bench":
        var generated = SampleData.Generate(100_000);
        Console.WriteLine($"Stopwatch на {generated.Count:N0} згенерованих виданнях (середнє з 5 запусків):");
        foreach (var r in ReportBenchmark.Run(generated, ScarceThreshold))
            Console.WriteLine($" {r.Variant,-34} рядків: {r.Rows,6} час: {r.Milliseconds,8:F2} мс");
        break;
    case "events":
        RunEventsDemo(args);
        break;
    case "import":
        await RunImportAsync(args);
        break;
    case "import-stream":
        await RunImportStreamAsync(args);
        break;
    default:
        Console.WriteLine("report-top | report-books | report-low | report-overdue | report-groups | report-sum | report-demo");
        Console.WriteLine("report-readers | report-query | report-bench | events");
        Console.WriteLine("import [шлях] [--timeout-ms=N]  (асинхронний імпорт, Ctrl+C скасовує)");
        Console.WriteLine("import-stream [шлях] [--timeout-ms=N] [--user-timeout-ms=N]  (IAsyncEnumerable + IProgress + зв'язані токени)");
        Console.WriteLine("прапорці: --empty (порожнє джерело), --file (файлове сховище для events)");
        break;
}

static async Task RunImportAsync(string[] args)
{
    var path = args.Where(a => !a.StartsWith("--")).Skip(1).FirstOrDefault() ?? "data/sample.json";

    var service = new LendingService(new InMemoryBookStore());
    var events = 0;
    service.Changed += (_, _) => events++;

    using var cts = new CancellationTokenSource();
    ConsoleCancelEventHandler onCancel = (_, e) => { e.Cancel = true; cts.Cancel(); };
    Console.CancelKeyPress += onCancel;

    var timeoutArg = args.FirstOrDefault(a => a.StartsWith("--timeout-ms="));
    if (timeoutArg is not null && int.TryParse(timeoutArg["--timeout-ms=".Length..], out var ms))
        cts.CancelAfter(TimeSpan.FromMilliseconds(ms));

    try
    {
        var added = await service.ImportAsync(new JsonBookImporter(), path, cts.Token);
        Console.WriteLine($"Імпортовано записів: {added} (подій: {events})");
    }
    catch (OperationCanceledException) { Console.WriteLine("Імпорт скасовано."); }
    catch (DomainException ex) { Console.WriteLine($"Дані відхилено: {ex.Message}"); }
    catch (FileNotFoundException) { Console.WriteLine($"Файл не знайдено: {path}"); }
    catch (JsonException ex) { Console.WriteLine($"Некоректний JSON: {ex.Message}"); }
    finally { Console.CancelKeyPress -= onCancel; }
}

static async Task RunImportStreamAsync(string[] args)
{
    var path = args.Where(a => !a.StartsWith("--")).Skip(1).FirstOrDefault() ?? "data/sample.json";

    var service = new LendingService(new InMemoryBookStore());

    // Токен від Ctrl+C
    using var userCts = new CancellationTokenSource();
    ConsoleCancelEventHandler onCancel = (_, e) => { e.Cancel = true; userCts.Cancel(); };
    Console.CancelKeyPress += onCancel;

    // Таймаут від --user-timeout-ms (якщо задано)
    using var timeoutCts = new CancellationTokenSource();
    var timeoutArg = args.FirstOrDefault(a => a.StartsWith("--user-timeout-ms="));
    if (timeoutArg is not null && int.TryParse(timeoutArg["--user-timeout-ms=".Length..], out var ms))
        timeoutCts.CancelAfter(TimeSpan.FromMilliseconds(ms));

    // Зв'язаний токен: спрацьовує від будь-якого з двох джерел
    using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(userCts.Token, timeoutCts.Token);

    var progress = new Progress<int>(n => Console.Write($"\r  імпортовано: {n} записів..."));

    try
    {
        var added = await service.ImportStreamAsync(new JsonStreamingImporter(), path, progress, linkedCts.Token);
        Console.WriteLine($"\nІмпортовано записів: {added}");
    }
    catch (OperationCanceledException)
    {
        var reason = timeoutCts.IsCancellationRequested ? "таймаут" : "Ctrl+C";
        Console.WriteLine($"\nІмпорт скасовано ({reason}).");
    }
    catch (DomainException ex) { Console.WriteLine($"\nДані відхилено: {ex.Message}"); }
    catch (FileNotFoundException) { Console.WriteLine($"\nФайл не знайдено: {path}"); }
    catch (JsonException ex) { Console.WriteLine($"\nНекоректний JSON: {ex.Message}"); }
    finally { Console.CancelKeyPress -= onCancel; }
}

static void PrintBooks(IReadOnlyList<BookLine> lines)
{
    if (lines.Count == 0) { Console.WriteLine(" (порожньо)"); return; }
    foreach (var l in lines)
        Console.WriteLine($" {l.Isbn,-15} {l.Title,-36} {l.Author,-26} {l.Copies,3}");
}

static void PrintReaders(IReadOnlyList<ReaderLine> lines)
{
    if (lines.Count == 0) { Console.WriteLine(" (порожньо)"); return; }
    foreach (var l in lines)
        Console.WriteLine($" {l.ReaderId,-6} видач: {l.Loans,3}");
}

static void PrintOverdue(IReadOnlyList<OverdueLine> lines)
{
    if (lines.Count == 0) { Console.WriteLine(" (порожньо)"); return; }
    foreach (var l in lines)
        Console.WriteLine($" {l.LoanId,-6} {l.CopyId,-7} {l.ReaderId,-6} до {l.DueDate:dd.MM.yyyy} прострочено днів: {l.DaysOverdue,3}");
}

static void PrintGroups(IReadOnlyList<GroupSummary> groups)
{
    if (groups.Count == 0) { Console.WriteLine(" (порожньо)"); return; }
    foreach (var g in groups)
        Console.WriteLine($" {g.Key,-11} видань: {g.Count,3} примірників: {g.TotalCopies,4}");
}

static void PrintActivity(IReadOnlyList<ReaderActivity> lines)
{
    if (lines.Count == 0) { Console.WriteLine(" (порожньо)"); return; }
    foreach (var l in lines)
        Console.WriteLine($" {l.ReaderId,-6} {l.FullName,-22} видач: {l.TotalLoans,3} на руках: {l.ActiveLoans,2}");
}

static void RunEventsDemo(string[] args)
{
    IBookStore store = StoreFactory.Create(args);
    var service = new LendingService(store);
    Console.WriteLine($"Сховище: {store.GetType().Name}\n");

    var logPath = Path.Combine(AppContext.BaseDirectory, "logs", "app.log");
    Directory.CreateDirectory(Path.GetDirectoryName(logPath)!);

    void OnLibraryChanged(object? sender, BookChangedEventArgs e)
    {
        var details = e.Kind switch
        {
            BookChangeKind.BookAdded => "додано новий примірник книги",
            BookChangeKind.CopyIssued => $"видано читачу {e.ReaderId} до {e.DueDate:dd.MM.yyyy}",
            BookChangeKind.CopyReturned => "повернено до фонду бібліотеки",
            _ => "невідома подія"
        };

        var line = $"{e.At:HH:mm:ss} {e.Kind,-13} {e.CopyId} -> {details}";
        Console.WriteLine(line);
        File.AppendAllText(logPath, line + Environment.NewLine);
    }

    service.Changed += OnLibraryChanged;

    int changes = 0;
    service.Changed += (_, _) => changes++;

    Console.WriteLine("=== Операції з двома підписниками ===");
    var created = service.AddBook("978-0451524935");
    service.IssueCopy(created.Id, "R-101");
    Console.WriteLine($"Подій отримано: {changes}\n");

    Console.WriteLine("=== Відписка від OnLibraryChanged та тиха операція ===");
    service.Changed -= OnLibraryChanged;

    service.ReturnCopy(created.Id);
    Console.WriteLine($"Подій отримано: {changes} (збільшилось лише у лямбди-лічильника)\n");

    Console.WriteLine("=== Сценарій відмови (порушення інваріанту) ===");
    try
    {
        service.ReturnCopy(created.Id);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[ПОМИЛКА]: {ex.Message}");
        Console.WriteLine($"Подій отримано: {changes} (подія не виникла через помилку)\n");
    }

    Console.WriteLine("=== Делегат Func: пошук доступних книг (FindAll) ===");
    var availableBooks = service.FindAll(copy => !copy.IsIssued);
    Console.WriteLine($"Знайдено доступних книг: {availableBooks.Count}");
    foreach (var book in availableBooks)
    {
        Console.WriteLine($" - {book}");
    }

    Console.WriteLine("\n=== Додаткове завдання 1: INotificationSink (ручний DI) ===");
    INotificationSink consoleSink = new ConsoleSink();
    var serviceWithSink = new LendingService(store, consoleSink);
    var bookWithSink = serviceWithSink.AddBook("978-0132350884");
    serviceWithSink.IssueCopy(bookWithSink.Id, "R-202");

    Console.WriteLine("\n=== Додаткове завдання 2: Action<string>");
    string csvPath = Path.Combine(AppContext.BaseDirectory, "data", "sample.csv");
    if (!File.Exists(csvPath))
    {
        csvPath = Path.GetFullPath("data/sample.csv");
    }

    if (File.Exists(csvPath))
    {
        Console.WriteLine($"Імпорт з {Path.GetFileName(csvPath)} із callback-звітом:");
        var importResult = BookCsvImporter.Load(csvPath, onSkippedLine: msg => Console.WriteLine($"   [Імпорт-звіт] {msg}"));
        Console.WriteLine($"Успішно імпортовано валідних сутностей: {importResult.Items.Count}");
    }
}
