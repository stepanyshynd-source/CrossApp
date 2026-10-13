using System;
using System.IO;
using System.Linq;
using Core.Abstractions;
using Core.Services;
using Core.Storage;

bool useFile = args.Contains("--file");
string dataPath = Path.Combine(AppContext.BaseDirectory, "data", "library.json");

IBookStore store = useFile
    ? new FileBookStore(dataPath)
    : new InMemoryBookStore();

var service = new LendingService(store);

Console.WriteLine($"Сховище: {store.GetType().Name}\n");

try
{
    var book1 = service.AddBook("978-0451524935");
    Console.WriteLine($"[+] Додано нову книгу з генерованим ID: {book1.Id}");

    service.IssueCopy(book1.Id);
    Console.WriteLine($"[+] Книгу {book1.Id} успішно видано читачу.");

    Console.WriteLine("\n--- Всі книги в каталозі ---");
    foreach (var b in service.All())
    {
        Console.WriteLine(b);
    }

    Console.WriteLine("\n--- Сценарій відмови ---");
    Console.WriteLine("Спроба видати ту саму книгу ще раз:");
    service.IssueCopy(book1.Id);
}
catch (Exception ex)
{
    Console.WriteLine($"[ПОМИЛКА]: {ex.Message}");
}