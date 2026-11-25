using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Core.Abstractions;
using Core.Domain;
using Core.Dto;

namespace Core.Import;

public sealed class JsonBookImporter : IBookImporter
{
    private static readonly JsonSerializerOptions Options = new() { PropertyNameCaseInsensitive = true };

    public async Task<IReadOnlyList<BookCopy>> LoadAsync(string path, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read,
            FileShare.Read, 4096, useAsync: true);

        var dtos = await JsonSerializer.DeserializeAsync<List<BookDto>>(stream, Options, ct)
            ?? throw new DomainException($"Файл {Path.GetFileName(path)} не містить списку.");

        var result = new List<BookCopy>(dtos.Count);
        for (var i = 0; i < dtos.Count; i++)
        {
            ct.ThrowIfCancellationRequested();

            var dto = dtos[i];
            if (dto is null)
                throw new DomainException($"Запис {i + 1}: порожній елемент.");

            try { result.Add(BookCopy.Create(dto.Id, dto.Isbn)); }
            catch (ArgumentException ex)
            { throw new DomainException($"Запис {i + 1}: {ex.Message}", ex); }
        }
        return result;
    }
}
