using System;
using System.Collections.Generic;
using Core.Domain;
using Core.Dto;

Console.WriteLine("=== Сценарій 1: успіх ===");

BookCopy copy = BookCopy.Create("B-001", "978-0451524935");
DateTime issueDate = new DateTime(2026, 10, 1);
Loan loan = Loan.Open("L-100", copy, "R-001", issueDate);
Console.WriteLine(loan);

DateTime returnDate = new DateTime(2026, 10, 5);
loan.Close(returnDate, copy);
Console.WriteLine(loan);


Console.WriteLine("\n=== Сценарій 2: порушення базових інваріантів ===");
TryDo("порожній ISBN", () => BookCopy.Create("B-002", "  "));

BookCopy copy2 = BookCopy.Create("B-003", "978-1234567890");
Loan loan2 = Loan.Open("L-101", copy2, "R-002", issueDate);
TryDo("видача вже виданого примірника", () => Loan.Open("L-102", copy2, "R-003", issueDate));


Console.WriteLine("\n=== Сценарій 3: ДОДАТКОВІ ЗАВДАННЯ ===");

BookCopy copy3 = BookCopy.Create("B-004", "978-1111111111");
TryDo("ліміт відкритих видач", () => Loan.Open("L-103", copy3, "R-004", issueDate, readerActiveLoans: 3));

TryDo("закриття закритої видачі", () => loan.Close(returnDate, copy));


Console.WriteLine("\n[Завдання 1: Конвертація ImportResult]");
var importedDtos = new List<BookCopyDto>
{
    new BookCopyDto("B-010", "978-555", false),
    new BookCopyDto("B-011", "   ", false),
    new BookCopyDto("B-012", "978-666", true)
};
var importResult = new ImportResult<BookCopyDto>(importedDtos, new List<string>());

var (validEntities, errors) = MapFromImportResult(importResult);
Console.WriteLine($"Успішно створено сутностей: {validEntities.Count}");
Console.WriteLine($"Кількість помилок інваріантів: {errors.Count}");
foreach (var err in errors) Console.WriteLine($" - {err}");



static (List<BookCopy> Valid, List<string> Errors) MapFromImportResult(ImportResult<BookCopyDto> result)
{
    var valid = new List<BookCopy>();
    var errors = new List<string>();

    foreach (var dto in result.Items)
    {
        try
        {
            valid.Add(BookCopy.FromDto(dto));
        }
        catch (Exception ex)
        {
            errors.Add($"Помилка створення {dto.Id}: {ex.Message}");
        }
    }
    return (valid, errors);
}

static void TryDo(string title, Action action)
{
    try
    {
        action();
        Console.WriteLine($" [Х] {title}: виняток НЕ спрацював — інваріант відсутній!");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"{title}: {ex.GetType().Name} - {ex.Message}");
    }
}