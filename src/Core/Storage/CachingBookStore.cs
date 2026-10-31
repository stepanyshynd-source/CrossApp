using System;
using System.Collections.Generic;
using Core.Abstractions;
using Core.Domain;

namespace Core.Storage;

public sealed class CachingBookStore(IBookStore innerStore) : IBookStore
{
    private readonly Dictionary<string, BookCopy> _localCache = new();
    private readonly IBookStore _inner = innerStore ?? throw new ArgumentNullException(nameof(innerStore));

    public BookCopy? GetById(string id)
    {
        if (_localCache.TryGetValue(id, out var cached)) return cached;

        var item = _inner.GetById(id);
        if (item != null) _localCache[id] = item;
        return item;
    }

    public void Add(BookCopy item) { _inner.Add(item); _localCache[item.Id] = item; }

    public void Update(BookCopy item) { _inner.Update(item); _localCache[item.Id] = item; }

    public bool Remove(string id) { _localCache.Remove(id); return _inner.Remove(id); }

    public IReadOnlyList<BookCopy> List() => _inner.List();

    public IEnumerable<BookCopy> Find(Func<BookCopy, bool> predicate) => _inner.Find(predicate);
}