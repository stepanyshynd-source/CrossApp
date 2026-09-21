namespace Core.Import;

using System.Text.Json;
using Core.Dto;

public static class BookJsonImporter
{
    public static ImportResult<IEntityDto> Load(string path)
    {
        var errors = new List<string>();
        try
        {
            string json = File.ReadAllText(path);
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var books = JsonSerializer.Deserialize<List<BookDto>>(json, options) ?? [];

            // Перетворюємо список BookDto на список IEntityDto
            var items = books.Cast<IEntityDto>().ToList();
            return new ImportResult<IEntityDto>(items, errors);
        }
        catch (Exception ex)
        {
            errors.Add($"JSON помилка: {ex.Message}");
            return new ImportResult<IEntityDto>([], errors);
        }
    }
}