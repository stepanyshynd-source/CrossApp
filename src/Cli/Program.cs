using Core;

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