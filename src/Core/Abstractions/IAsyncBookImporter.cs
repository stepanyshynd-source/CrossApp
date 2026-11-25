using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using Core.Domain;

namespace Core.Abstractions;

public interface IAsyncBookImporter
{
    IAsyncEnumerable<BookCopy> StreamAsync(string path, [EnumeratorCancellation] CancellationToken ct = default);
}
