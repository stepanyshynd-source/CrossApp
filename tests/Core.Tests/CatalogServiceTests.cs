using System;
using System.Collections.Generic;
using Core.Events;
using Core.Services;
using Core.Storage;
using Xunit;

namespace Core.Tests;

public class CatalogServiceTests
{
    [Fact] // інваріант: відмова, незмінний стан, подія не піднялась
    public void IssueCopy_WhenAlreadyIssued_Throws_AndKeepsState()
    {
        var store = new InMemoryBookStore();
        var service = new LendingService(store);
        var copy = service.AddBook("978-0451524935");
        service.IssueCopy(copy.Id, "R-1");

        var events = 0;
        service.Changed += (_, _) => events++;

        Assert.Throws<DomainException>(() => service.IssueCopy(copy.Id, "R-2"));

        Assert.True(store.GetById(copy.Id)!.IsIssued);
        Assert.Equal(0, events);
    }

    [Fact] // подія з тижня 6 як спосіб перевірки
    public void AddBook_RaisesChangedExactlyOnce()
    {
        var service = new LendingService(new InMemoryBookStore());
        var events = new List<BookChangedEventArgs>();
        service.Changed += (_, e) => events.Add(e);

        service.AddBook("978-0132350884");

        Assert.Single(events);
        Assert.Equal(BookChangeKind.BookAdded, events[0].Kind);
    }

    [Fact] // ArgumentException для некоректного аргументу
    public void AddBook_WithEmptyIsbn_ThrowsArgumentException_AndStoreStaysEmpty()
    {
        var store = new InMemoryBookStore();
        var service = new LendingService(store);

        Assert.Throws<ArgumentException>(() => service.AddBook("  "));

        Assert.Empty(store.List());
    }

    [Fact] // варіант «Бібліотека»: повернення невиданого примірника
    public void ReturnCopy_WhenNotIssued_ThrowsDomainException()
    {
        var service = new LendingService(new InMemoryBookStore());
        var copy = service.AddBook("978-0596007126");

        Assert.Throws<DomainException>(() => service.ReturnCopy(copy.Id));
    }
}
