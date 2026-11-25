using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Core.Abstractions;
using Core.Domain;
using Core.Events;

namespace Core.Services;

public sealed class LendingService(IBookStore store, INotificationSink? sink = null)
{
    private readonly IBookStore _store = store ?? throw new ArgumentNullException(nameof(store));
    private readonly INotificationSink? _sink = sink;

    public event EventHandler<BookChangedEventArgs>? Changed;

    private void OnChanged(BookCopy copy, BookChangeKind kind, string? readerId = null, DateTime? dueDate = null)
    {
        Changed?.Invoke(this, new BookChangedEventArgs(copy.Id, kind, readerId, dueDate));
        _sink?.Notify($"Подія {kind}: примірник {copy.Id}");
    }

    public BookCopy AddBook(string isbn)
    {
        string id = "B-" + Guid.NewGuid().ToString("N")[..5].ToUpper();
        var copy = BookCopy.Create(id, isbn);
        _store.Add(copy);
        OnChanged(copy, BookChangeKind.BookAdded);
        return copy;
    }

    public void IssueCopy(string id, string readerId = "R-001", DateTime? dueDate = null)
    {
        var copy = _store.GetById(id) ?? throw new DomainException($"Немає запису з id={id}.");
        copy.Issue();
        _store.Update(copy);
        dueDate ??= DateTime.Today.AddDays(14);
        OnChanged(copy, BookChangeKind.CopyIssued, readerId, dueDate);
    }

    public void ReturnCopy(string id)
    {
        var copy = _store.GetById(id) ?? throw new DomainException($"Немає запису з id={id}.");
        copy.Return();
        _store.Update(copy);
        OnChanged(copy, BookChangeKind.CopyReturned);
    }

    public async Task<int> ImportAsync(IBookImporter importer, string path, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(importer);

        var items = await importer.LoadAsync(path, ct);
        var added = 0;
        foreach (var copy in items)
        {
            ct.ThrowIfCancellationRequested();
            if (_store.GetById(copy.Id) is not null) continue; // ідемпотентність імпорту
            _store.Add(copy);
            OnChanged(copy, BookChangeKind.BookAdded);
            added++;
        }
        return added;
    }

    public async Task<int> ImportStreamAsync(
        IAsyncBookImporter importer,
        string path,
        IProgress<int>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(importer);

        var added = 0;
        await foreach (var copy in importer.StreamAsync(path, cancellationToken))
        {
            if (_store.GetById(copy.Id) is not null) continue; // ідемпотентність
            _store.Add(copy);
            OnChanged(copy, BookChangeKind.BookAdded);
            added++;
            progress?.Report(added);
        }
        return added;
    }

    public IReadOnlyList<BookCopy> All() => _store.List();

    public BookCopy? Find(string id) => _store.GetById(id);

    public IReadOnlyList<BookCopy> FindAll(Func<BookCopy, bool> predicate) =>
        _store.List().Where(predicate).ToList();

    public IEnumerable<BookCopy> FindAvailable() => _store.Find(copy => !copy.IsIssued);
}