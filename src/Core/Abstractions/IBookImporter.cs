using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Core.Domain;

namespace Core.Abstractions;

public interface IBookImporter
{
    Task<IReadOnlyList<BookCopy>> LoadAsync(string path, CancellationToken ct = default);
}
