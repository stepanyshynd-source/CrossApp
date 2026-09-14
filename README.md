# CrossApp
 Наскрізний проєкт з крос-платформного програмування.
 Предметна область: Бібліотека. Сутності: Book, BookCopy, Reader, Loan.
 Призначення: облік видач примірників книг читачам.
 ## Запуск
 dotnet build
 dotnet run --project src/Cli
 ## Середовище
 .NET SDK 10.0, macOS arm64
 ## Додаткове завдання номер 1
 Розмір каталогу publish з win-x64 RID = 77мб.
 Розмір каталогу publish з osx-arm64 RID = 79мб

## Структура рішення
CrossApp/
├── CrossApp.sln
├── README.md
├── .gitignore
└── src/
    ├── Core/
    │   ├── Core.csproj
    │   └── EnvironmentInfo.cs
    └── Cli/
        ├── Cli.csproj
        └── Program.cs

## Публікація self-contained 
dotnet publish src/Cli -c Release -r osx-arm64 --self-contained true -o publish-sc

## Публікація Framework-dependent
dotnet publish src/Cli -c Release -r osx-arm64 --self-contained false -o publish-fd

## таблиця «RID – режим – розмір – чи потрібен встановлений runtime»
RID	              Режим                       Розмір       Чи потрібен встановлений runtime
osx-arm64         self-contained              79 МБ	       ні
osx-arm64         framework-dependent	      168 КБ	   так 
