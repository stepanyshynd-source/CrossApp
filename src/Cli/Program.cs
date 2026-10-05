using System;
using Core.Domain;

Console.WriteLine("=== Сценарій 1: успіх ===");

BookCopy copy = BookCopy.Create("B-001", "978-0451524935");
Console.WriteLine(copy);

DateTime issueDate = new DateTime(2026, 10, 1);
Loan loan = Loan.Open("L-100", copy, "R-001", issueDate);
Console.WriteLine(loan);
Console.WriteLine($"Статус книги після видачі: {copy}");

DateTime returnDate = new DateTime(2026, 10, 5);
loan.Close(returnDate, copy);
Console.WriteLine(loan);
Console.WriteLine($"Статус книги після повернення: {copy}");


Console.WriteLine("\n=== Сценарій 2: порушення інваріантів ===");

TryDo("порожній ISBN", () => BookCopy.Create("B-002", "  "));

BookCopy copy2 = BookCopy.Create("B-003", "978-1234567890");
Loan loan2 = Loan.Open("L-101", copy2, "R-002", issueDate);

TryDo("видача вже виданого примірника", () => Loan.Open("L-102", copy2, "R-003", issueDate));

TryDo("повернення в минулому", () => loan2.Close(new DateTime(2026, 9, 20), copy2));


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