using System;
using System.Collections.Generic;
using System.Linq;

namespace Core.Reports;

public static class EnumerableExtensions
{
    public static IEnumerable<T> TopBy<T>(this IEnumerable<T> source, Func<T, int> keySelector, Func<T, string> tieBreaker, int n)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(keySelector);
        ArgumentNullException.ThrowIfNull(tieBreaker);

        return source.OrderByDescending(keySelector)
                     .ThenBy(tieBreaker, StringComparer.Ordinal)
                     .Take(n);
    }
}
