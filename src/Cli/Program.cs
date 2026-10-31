using System;
using System.IO;
using System.Linq;
using Cli.Notifications;
using Core.Abstractions;
using Core.Events;
using Core.Import;
using Core.Services;
using Core.Storage;

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