using System.Collections.Generic;
using System.Linq;
using Core.Domain;

namespace Core.Reports;

public static class DeferredExecutionDemo
{
    public static DeferredDemoResult Run(IEnumerable<Book> books, int threshold)
    {
        var source = books.ToList();
        var query = source.Where(b => b.Copies < threshold);
        int before = query.Count();

        source.Add(Book.Create("978-0000000001", "Тимчасове видання", "Демо", 2024, "Роман", 1));
        int after = query.Count();

        var snapshot = query.ToList();
        source.Clear();

        return new DeferredDemoResult(before, after, snapshot.Count, query.Count());
    }
}
