using Core;
using Core.Dto;
using Core.Import;

//1-2 лаби
EnvironmentReport report = EnvironmentInfo.Collect();

Console.WriteLine("CrossApp – практикум з крос-платформного програмування");
Console.WriteLine("Студент: Степанишин Данило, група ФЕІ-31");
Console.WriteLine(new string('-', 52));
Console.WriteLine($"ОС (OSDescription)  : {report.OsDescription}");
Console.WriteLine($"ОС (Environment)    : {report.OsVersion}");
Console.WriteLine($"Архітектура процесу : {report.ProcessArchitecture}");
Console.WriteLine($"Версія .NET (CLR)   : {report.ClrVersion}");
Console.WriteLine($"Runtime             : {report.FrameworkDescription}");
Console.WriteLine($"Каталог застосунку  : {report.BaseDirectory}");
Console.WriteLine($"Поточний каталог    : {report.CurrentDirectory}");
Console.WriteLine($"Примітка збірки     : {report.BuildNote}");
Console.WriteLine(new string('-', 52));
Console.WriteLine("Предметна область: Бібліотека (видання, примірник, читач, видача)");

//3 лаба
string path = args.Length > 0 ? args[0] : Path.Combine("data", "sample.csv");

if (!File.Exists(path))
{
    Console.WriteLine($"Файл не знайдено: {Path.GetFullPath(path)}");
    return 1;
}

string ext = Path.GetExtension(path).ToLowerInvariant();
ImportResult<IEntityDto> result = ext switch
{
    ".json" => BookJsonImporter.Load(path),
    _ => BookCsvImporter.Load(path)
};

Console.WriteLine($"Завантажено записів: {result.Items.Count}");

foreach (IEntityDto item in result.Items) // Збільшив Take до 8, щоб влізли читачі
{
    switch (item)
    {
        case BookDto b:
            Console.WriteLine($" [Книга] {b.Id,-6} {b.Isbn,-15} {b.Title,-26} {b.Year,4} {b.Author}");
            break;
        case ReaderDto r:
            Console.WriteLine($" [Читач] {r.Id,-6} {r.FullName,-25} {r.Phone}");
            break;
    }
}

int total = result.Items.Count + result.Errors.Count;
double percent = total > 0 ? Math.Round((double)result.Errors.Count / total * 100, 1) : 0;
Console.WriteLine($"\n[Статистика] Усього: {total} | Прийнято: {result.Items.Count} | Пропущено: {result.Errors.Count} ({percent}% помилок)");

return 0;