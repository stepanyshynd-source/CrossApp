using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Core.Domain;

namespace Core.Reports;

public static class ReportBenchmark
{
    public static IReadOnlyList<BenchmarkResult> Run(IReadOnlyList<Book> source, int threshold, int iterations = 5)
    {
        return
        [
            Measure("Ланцюжок без ToList у середині", iterations, () =>
                source.Where(b => b.Copies < threshold)
                      .OrderBy(b => b.Copies).ThenBy(b => b.Isbn, StringComparer.Ordinal)
                      .Select(ReportService.ToLine)
                      .ToList().Count),

            Measure("ToList після кожного кроку", iterations, () =>
                source.Where(b => b.Copies < threshold).ToList()
                      .OrderBy(b => b.Copies).ThenBy(b => b.Isbn, StringComparer.Ordinal).ToList()
                      .Select(ReportService.ToLine).ToList()
                      .Count),

            Measure("OrderBy до Where (помилка)", iterations, () =>
                source.OrderBy(b => b.Copies).ThenBy(b => b.Isbn, StringComparer.Ordinal)
                      .Where(b => b.Copies < threshold)
                      .Select(ReportService.ToLine)
                      .ToList().Count),

            Measure("ByGenre (синтаксис методів)", iterations, () => ReportService.ByGenre(source).Count),

            Measure("ByGenreQuery (синтаксис запитів)", iterations, () => ReportService.ByGenreQuery(source).Count),
        ];
    }

    private static BenchmarkResult Measure(string variant, int iterations, Func<int> run)
    {
        run();
        int rows = 0;
        var stopwatch = Stopwatch.StartNew();

        for (int i = 0; i < iterations; i++)
            rows = run();

        stopwatch.Stop();
        return new BenchmarkResult(variant, rows, stopwatch.Elapsed.TotalMilliseconds / iterations);
    }
}
