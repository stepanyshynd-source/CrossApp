using System;

namespace Core.Domain;

public sealed class Book
{
    public string Isbn { get; }
    public string Title { get; }
    public string Author { get; }
    public int Year { get; }
    public string Genre { get; }
    public int Copies { get; }

    private Book(string isbn, string title, string author, int year, string genre, int copies)
    {
        Isbn = isbn;
        Title = title;
        Author = author;
        Year = year;
        Genre = genre;
        Copies = copies;
    }

    public static Book Create(string isbn, string title, string author, int year, string genre, int copies)
    {
        if (string.IsNullOrWhiteSpace(isbn))
            throw new ArgumentException("ISBN не може бути порожнім", nameof(isbn));

        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Назва видання обов'язкова", nameof(title));

        if (string.IsNullOrWhiteSpace(author))
            throw new ArgumentException("Автор обов'язковий", nameof(author));

        if (string.IsNullOrWhiteSpace(genre))
            throw new ArgumentException("Жанр обов'язковий", nameof(genre));

        if (year < 1450 || year > DateTime.Today.Year)
            throw new ArgumentOutOfRangeException(nameof(year), year, "Рік видання поза допустимими межами");

        if (copies < 0)
            throw new ArgumentOutOfRangeException(nameof(copies), copies, "Кількість примірників не може бути від'ємною");

        return new Book(isbn.Trim(), title.Trim(), author.Trim(), year, genre.Trim(), copies);
    }

    public override string ToString() =>
        $"{Isbn} «{Title}» ({Author}, {Year}) [{Genre}] - примірників: {Copies}";
}
