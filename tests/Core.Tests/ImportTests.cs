using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Core.Abstractions;
using Core.Domain;
using Core.Events;
using Core.Import;
using Core.Services;
using Core.Storage;
using Xunit;

namespace Core.Tests;

public class ImportTests
{
    private sealed class StubImporter(IReadOnlyList<BookCopy> items) : IBookImporter
    {
        public Task<IReadOnlyList<BookCopy>> LoadAsync(string path, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();
            return Task.FromResult(items);
        }
    }

    [Fact] // скасування
    public async Task ImportAsync_WhenAlreadyCancelled_Throws()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();
        var service = new LendingService(new InMemoryBookStore());

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => service.ImportAsync(new JsonBookImporter(), "any.json", cts.Token));
    }

    [Fact] // подія на кожен доданий запис + ідемпотентність
    public async Task ImportAsync_AddsNewItems_RaisesEventPerItem_AndIsIdempotent()
    {
        IReadOnlyList<BookCopy> items = [BookCopy.Create("B-1", "978-1"), BookCopy.Create("B-2", "978-2")];
        var store = new InMemoryBookStore();
        var service = new LendingService(store);
        var events = new List<BookChangedEventArgs>();
        service.Changed += (_, e) => events.Add(e);
        var importer = new StubImporter(items);

        var first = await service.ImportAsync(importer, "ignored.json");
        var second = await service.ImportAsync(importer, "ignored.json");

        Assert.Equal(2, first);
        Assert.Equal(0, second);
        Assert.Equal(2, events.Count);
        Assert.Equal(2, store.List().Count);
    }

    [Fact] // зіпсований запис; тимчасовий файл видаляється у finally
    public async Task LoadAsync_WithInvalidRecord_ThrowsDomainException_WithRecordNumber()
    {
        const string json = """
            [
              { "Id": "B-1", "Isbn": "978-1", "Title": "A", "Year": 2000 },
              { "Id": "B-2", "Isbn": "",      "Title": "B", "Year": 2001 }
            ]
            """;
        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.json");
        try
        {
            await File.WriteAllTextAsync(path, json);

            var ex = await Assert.ThrowsAsync<DomainException>(
                () => new JsonBookImporter().LoadAsync(path));

            Assert.Contains("Запис 2", ex.Message);
            Assert.IsAssignableFrom<ArgumentException>(ex.InnerException);
        }
        finally
        {
            File.Delete(path);
        }
    }

    // ── Додаткові завдання ──────────────────────────────────────────────────

    private sealed class StubAsyncImporter(IReadOnlyList<BookCopy> items) : IAsyncBookImporter
    {
        public async IAsyncEnumerable<BookCopy> StreamAsync(
            string path,
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
        {
            foreach (var item in items)
            {
                ct.ThrowIfCancellationRequested();
                await Task.Yield();
                yield return item;
            }
        }
    }

    [Fact] // IAsyncEnumerable: потоковий імпорт та ідемпотентність
    public async Task ImportStreamAsync_AddsNewItems_AndIsIdempotent()
    {
        IReadOnlyList<BookCopy> items = [BookCopy.Create("S-1", "978-1"), BookCopy.Create("S-2", "978-2")];
        var store = new InMemoryBookStore();
        var service = new LendingService(store);
        var importer = new StubAsyncImporter(items);

        var first  = await service.ImportStreamAsync(importer, "ignored.json");
        var second = await service.ImportStreamAsync(importer, "ignored.json");

        Assert.Equal(2, first);
        Assert.Equal(0, second);
        Assert.Equal(2, store.List().Count);
    }

    [Fact] // IProgress<int> отримує звіт за кожен доданий запис
    public async Task ImportStreamAsync_ReportsProgress()
    {
        IReadOnlyList<BookCopy> items =
        [
            BookCopy.Create("P-1", "978-1"),
            BookCopy.Create("P-2", "978-2"),
            BookCopy.Create("P-3", "978-3"),
        ];
        var service = new LendingService(new InMemoryBookStore());
        var reported = new List<int>();
        // SynchronousProgress викликає callback inline, без пулу потоків
        var progress = new SynchronousProgress<int>(n => reported.Add(n));

        await service.ImportStreamAsync(new StubAsyncImporter(items), "ignored.json", progress);

        Assert.Equal([1, 2, 3], reported);
    }

    /// <summary>Викликає callback синхронно — гарантований порядок без Task.Delay.</summary>
    private sealed class SynchronousProgress<T>(Action<T> handler) : IProgress<T>
    {
        public void Report(T value) => handler(value);
    }

    [Fact] // Зв'язані токени: спрацьовує таймаут, а не Ctrl+C
    public async Task LinkedTokens_TimeoutCancels_BeforeUserToken()
    {
        // Токен "користувача" — ніколи не скасовується вручну
        using var userCts    = new CancellationTokenSource();
        // Таймаут — вже скасований (симулює миттєве спрацювання)
        using var timeoutCts = new CancellationTokenSource();
        await timeoutCts.CancelAsync();

        // Зв'язаний токен: скасований через timeoutCts
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
            userCts.Token, timeoutCts.Token);

        var service = new LendingService(new InMemoryBookStore());

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => service.ImportStreamAsync(
                new StubAsyncImporter([BookCopy.Create("T-1", "978-0")]),
                "ignored.json",
                cancellationToken: linkedCts.Token));

        // Доводимо, що таймаут спрацював, а не Ctrl+C
        Assert.True(timeoutCts.IsCancellationRequested);
        Assert.False(userCts.IsCancellationRequested);
    }

}
