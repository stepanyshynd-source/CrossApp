using System;
using System.Collections.Generic;
using System.Linq;
using Core.Domain;
using Core.Dto;

namespace Core;

public static class SampleData
{
    private static readonly string[] Genres = ["Роман", "Фентезі", "Фантастика", "Поезія", "Антиутопія"];

    public static IReadOnlyList<Book> Books() =>
    [
        Book.Create("978-0451524935", "1984", "Джордж Орвелл", 1949, "Антиутопія", 7),
        Book.Create("978-0060850524", "Прекрасний новий світ", "Олдос Гакслі", 1932, "Антиутопія", 4),
        Book.Create("978-1451673319", "451° за Фаренгейтом", "Рей Бредбері", 1953, "Антиутопія", 2),
        Book.Create("978-0141439518", "Гордість і упередження", "Джейн Остін", 1813, "Роман", 5),
        Book.Create("978-0743273565", "Великий Гетсбі", "Френсіс Скотт Фіцджеральд", 1925, "Роман", 3),
        Book.Create("978-9660301234", "Тигролови", "Іван Багряний", 1944, "Роман", 6),
        Book.Create("978-9660304556", "Захар Беркут", "Іван Франко", 1883, "Роман", 4),
        Book.Create("978-0553103540", "Гра престолів", "Джордж Р.Р. Мартін", 1996, "Фентезі", 8),
        Book.Create("978-0618260300", "Гобіт", "Дж.Р.Р. Толкін", 1937, "Фентезі", 6),
        Book.Create("978-0618640157", "Володар перснів", "Дж.Р.Р. Толкін", 1954, "Фентезі", 5),
        Book.Create("978-0439708180", "Гаррі Поттер і філософський камінь", "Дж.К. Роулінг", 1997, "Фентезі", 9),
        Book.Create("978-0345391803", "Автостопом по галактиці", "Дуглас Адамс", 1979, "Фантастика", 3),
        Book.Create("978-0441172719", "Дюна", "Френк Герберт", 1965, "Фантастика", 5),
        Book.Create("978-0553293357", "Фундація", "Айзек Азімов", 1951, "Фантастика", 1),
        Book.Create("978-0765382030", "Задача трьох тіл", "Лю Цисінь", 2008, "Фантастика", 4),
        Book.Create("978-1503290563", "Кобзар", "Тарас Шевченко", 1840, "Поезія", 7),
        Book.Create("978-9660312345", "Лісова пісня", "Леся Українка", 1911, "Поезія", 2),
        Book.Create("978-9660398765", "Енеїда", "Іван Котляревський", 1798, "поезія", 3),
    ];

    public static IReadOnlyList<ReaderDto> Readers() =>
    [
        new("R-001", "Іван Франко", "+380501234567"),
        new("R-002", "Леся Українка", "+380671234567"),
        new("R-003", "Михайло Коцюбинський", "+380631112233"),
        new("R-004", "Ольга Кобилянська", "+380662223344"),
        new("R-005", "Василь Стефаник", "+380933334455"),
        new("R-006", "Марко Вовчок", "+380974445566"),
        new("R-007", "Панас Мирний", "+380505556677"),
        new("R-008", "Григорій Сковорода", "+380686667788"),
    ];

    private static readonly LoanSeed[] LoanSeeds =
    [
        new("L-001", "C-01-1", "R-001", 40, 14, 28),
        new("L-002", "C-08-1", "R-001", 20, 14),
        new("L-003", "C-11-1", "R-001", 5, 14),
        new("L-004", "C-09-1", "R-001", 60, 21, 45),
        new("L-005", "C-16-1", "R-002", 30, 14),
        new("L-006", "C-04-1", "R-002", 10, 14),
        new("L-007", "C-13-1", "R-002", 50, 14, 40),
        new("L-008", "C-02-1", "R-003", 25, 14),
        new("L-009", "C-12-1", "R-003", 3, 14),
        new("L-010", "C-14-1", "R-004", 15, 7),
        new("L-011", "C-10-1", "R-004", 90, 30, 70),
        new("L-012", "C-01-2", "R-005", 2, 14),
        new("L-013", "C-11-2", "R-005", 35, 14, 25),
        new("L-014", "C-08-2", "R-005", 70, 14, 60),
        new("L-015", "C-06-1", "R-006", 12, 14),
        new("L-016", "C-17-1", "R-007", 45, 14, 33),
        new("L-017", "C-03-1", "R-001", 100, 14, 90),
    ];

    public static IReadOnlyList<BookCopy> Copies() =>
        Books().SelectMany((book, index) => Enumerable.Range(1, book.Copies)
                                                      .Select(n => BookCopy.Create($"C-{index + 1:00}-{n}", book.Isbn)))
               .ToList();

    public static IReadOnlyList<Loan> Loans(DateTime today)
    {
        var copies = Copies().ToDictionary(c => c.Id, StringComparer.Ordinal);
        return LoanSeeds.Select(seed => OpenLoan(seed, copies[seed.CopyId], today)).ToList();
    }

    public static IReadOnlyList<Book> Generate(int count, int seed = 42)
    {
        var random = new Random(seed);
        return Enumerable.Range(1, count)
            .Select(i => Book.Create($"GEN-{i:000000}", $"Книга {i}", $"Автор {i % 500}",
                                     random.Next(1800, 2025), Genres[random.Next(Genres.Length)], random.Next(0, 20)))
            .ToList();
    }

    private static Loan OpenLoan(LoanSeed seed, BookCopy copy, DateTime today)
    {
        var issuedOn = today.AddDays(-seed.IssuedDaysAgo);
        var loan = Loan.Open(seed.Id, copy, seed.ReaderId, issuedOn, dueDate: issuedOn.AddDays(seed.LoanDays));

        if (seed.ReturnedDaysAgo is int returnedDaysAgo)
            loan.Close(today.AddDays(-returnedDaysAgo), copy);

        return loan;
    }

    private sealed record LoanSeed(string Id, string CopyId, string ReaderId, int IssuedDaysAgo, int LoanDays, int? ReturnedDaysAgo = null);
}
