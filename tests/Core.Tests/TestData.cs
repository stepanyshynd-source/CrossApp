using System.Collections.Generic;
using Core.Domain;

namespace Core.Tests;

internal static class TestData
{
    public static IReadOnlyList<Book> Books() =>
    [
        Book.Create("ISBN-BIG", "Велика книга",     "Автор А", 2000, "Наука", 50),
        Book.Create("ISBN-MID", "Середня книга",    "Автор Б", 2005, "Проза", 20),
        Book.Create("ISBN-LOW", "Мало примірників", "Автор В", 2010, "Проза", 2),
        Book.Create("ISBN-ONE", "Один примірник",   "Автор Г", 2015, "Наука", 1),
    ];
}
