using System;
using System.Collections.Generic;
using System.Linq;
using Core.Domain;
using Core.Dto;

namespace Core.Reports;

public static class ReportService
{
    public static IReadOnlyList<ReaderLine> TopReaders(IEnumerable<Loan> source, int n) =>
        source.GroupBy(l => l.ReaderId, StringComparer.Ordinal)
              .Select(g => new ReaderLine(g.Key, g.Count()))
              .TopBy(r => r.Loans, r => r.ReaderId, n)
              .ToList();

    public static IReadOnlyList<BookLine> TopBooks(IEnumerable<Book> source, int n) =>
        source.TopBy(b => b.Copies, b => b.Isbn, n)
              .Select(ToLine)
              .ToList();

    public static IReadOnlyList<BookLine> ScarceBooks(IEnumerable<Book> source, int threshold) =>
        source.Where(b => b.Copies < threshold)
              .OrderBy(b => b.Copies).ThenBy(b => b.Isbn, StringComparer.Ordinal)
              .Select(ToLine)
              .ToList();

    public static IReadOnlyList<OverdueLine> Overdue(IEnumerable<Loan> source, DateTime today) =>
        source.Where(l => l.IsOverdue(today))
              .OrderBy(l => l.DueDate).ThenBy(l => l.Id, StringComparer.Ordinal)
              .Select(l => new OverdueLine(l.Id, l.CopyId, l.ReaderId, l.DueDate, (today - l.DueDate).Days))
              .ToList();

    public static IReadOnlyList<GroupSummary> ByGenre(IEnumerable<Book> source) =>
        source.GroupBy(b => b.Genre, StringComparer.OrdinalIgnoreCase)
              .Select(g => new GroupSummary(g.Key, g.Count(), g.Sum(b => b.Copies)))
              .OrderByDescending(s => s.TotalCopies).ThenBy(s => s.Key, StringComparer.Ordinal)
              .ToList();

    public static IReadOnlyList<GroupSummary> ByGenreQuery(IEnumerable<Book> source) =>
        (from book in source
         group book by book.Genre.ToUpperInvariant() into g
         let key = g.First().Genre
         let total = g.Sum(b => b.Copies)
         orderby total descending, key
         select new GroupSummary(key, g.Count(), total))
        .ToList();

    public static IReadOnlyList<ReaderActivity> ReadersActivity(IEnumerable<ReaderDto> readers, IEnumerable<Loan> loans) =>
        readers.GroupJoin(loans,
                          reader => reader.Id,
                          loan => loan.ReaderId,
                          (reader, readerLoans) => new ReaderActivity(
                              reader.Id,
                              reader.FullName,
                              readerLoans.Count(),
                              readerLoans.Count(l => l.Status == LoanStatus.Active)))
               .OrderByDescending(a => a.TotalLoans).ThenBy(a => a.ReaderId, StringComparer.Ordinal)
               .ToList();

    public static (int Items, int Total, double Average, int Max) Totals(IEnumerable<Book> source)
    {
        var list = source.ToList();
        return list.Count == 0
            ? (0, 0, 0d, 0)
            : (list.Count, list.Sum(b => b.Copies), list.Average(b => b.Copies), list.Max(b => b.Copies));
    }

    internal static BookLine ToLine(Book book) => new(book.Isbn, book.Title, book.Author, book.Copies);
}
