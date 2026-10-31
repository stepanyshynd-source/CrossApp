namespace Core.Events;

using System;

public sealed class BookChangedEventArgs(
    string copyId,
    BookChangeKind kind,
    string? readerId = null,
    DateTime? dueDate = null) : EventArgs
{
    public string CopyId { get; } = copyId;
    public BookChangeKind Kind { get; } = kind;
    public string? ReaderId { get; } = readerId;
    public DateTime? DueDate { get; } = dueDate;
    public DateTimeOffset At { get; } = DateTimeOffset.Now;
}
