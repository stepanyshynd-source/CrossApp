namespace Core.Import;

using System.Globalization;
using Core.Dto;

public static class BookCsvImporter
{
    private const char Separator = ';';
    public static ImportResult<IEntityDto> Load(string path)
    {
        var items = new List<IEntityDto>();
        var errors = new List<string>();
        string[] lines = File.ReadAllLines(path);

        for (int i = 0; i < lines.Length; i++)
        {
            int number = i + 1;
            string line = lines[i];

            if (string.IsNullOrWhiteSpace(line) || line.StartsWith('#') || (number == 1 && line.StartsWith("id", StringComparison.OrdinalIgnoreCase)))
                continue;

            switch (ParseLine(line))
            {
                case ParseOk ok:
                    items.Add(ok.Value);
                    break;
                case ParseFailed failed:
                    errors.Add($"рядок {number}: {failed.Reason}");
                    break;
            }
        }
        return new ImportResult<IEntityDto>(items, errors);
    }

    private static ParseOutcome ParseLine(string line)
    {
        string[] parts = line.Split(Separator, StringSplitOptions.TrimEntries);

        return parts switch
        {
            { Length: < 3 } => new ParseFailed($"очікую мінімум 3 колонки, отримав {parts.Length}"),

            [var id, var isbn, var title, ..] when id.StartsWith("B", StringComparison.OrdinalIgnoreCase) && (isbn == "" || title == "")
                => new ParseFailed("ISBN або назва порожні"),

            [var id, _, _, var year, ..] when id.StartsWith("B", StringComparison.OrdinalIgnoreCase) && (!int.TryParse(year, out int y) || y < 1450 || y > DateTime.Now.Year)
                => new ParseFailed($"рік '{year}' поза допустимими межами"),

            [var id, var isbn, var title, var year] when id.StartsWith("B", StringComparison.OrdinalIgnoreCase)
                => new ParseOk(new BookDto(id, isbn, title, int.Parse(year, CultureInfo.InvariantCulture))),

            [var id, var isbn, var title, var year, var author] when id.StartsWith("B", StringComparison.OrdinalIgnoreCase)
                => new ParseOk(new BookDto(id, isbn, title, int.Parse(year, CultureInfo.InvariantCulture), author)),

            [var id, var name, var phone] when id.StartsWith("R", StringComparison.OrdinalIgnoreCase)
                => new ParseOk(new ReaderDto(id, name, phone)),

            _ => new ParseFailed($"невідомий формат або тип сутності: {parts[0]}")
        };
    }

    private abstract record ParseOutcome;
    private sealed record ParseOk(IEntityDto Value) : ParseOutcome;
    private sealed record ParseFailed(string Reason) : ParseOutcome;
}