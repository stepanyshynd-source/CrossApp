using System;
using System.Collections.Generic;
using System.Linq;
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
        var copy = _store.GetById(id) ?? throw new InvalidOperationException($"Немає запису з id={id}.");
        copy.Issue();
        _store.Update(copy);
        dueDate ??= DateTime.Today.AddDays(14);
        OnChanged(copy, BookChangeKind.CopyIssued, readerId, dueDate);
    }

    public void ReturnCopy(string id)
    {
        var copy = _store.GetById(id) ?? throw new InvalidOperationException($"Немає запису з id={id}.");
        copy.Return();
        _store.Update(copy);
        OnChanged(copy, BookChangeKind.CopyReturned);
    }

    public IReadOnlyList<BookCopy> All() => _store.List();

    public BookCopy? Find(string id) => _store.GetById(id);

    public IReadOnlyList<BookCopy> FindAll(Func<BookCopy, bool> predicate) =>
        _store.List().Where(predicate).ToList();

    public IEnumerable<BookCopy> FindAvailable() => _store.Find(copy => !copy.IsIssued);
}