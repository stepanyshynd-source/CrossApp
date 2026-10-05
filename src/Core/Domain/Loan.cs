using System;
using Core.Dto;

namespace Core.Domain;

public enum LoanStatus
{
    Active,
    Returned,
    Lost
}

public sealed class Loan
{
    public string Id { get; }
    public string CopyId { get; }
    public string ReaderId { get; }
    public DateTime IssuedOn { get; }
    public DateTime? ReturnedOn { get; private set; }

    public LoanStatus Status { get; private set; }

    private Loan(string id, string copyId, string readerId, DateTime issuedOn, DateTime? returnedOn = null, LoanStatus status = LoanStatus.Active)
    {
        Id = id;
        CopyId = copyId;
        ReaderId = readerId;
        IssuedOn = issuedOn;
        ReturnedOn = returnedOn;
        Status = status;
    }

    public static Loan Open(string id, BookCopy copy, string readerId, DateTime issuedOn, int readerActiveLoans = 0)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("Ідентифікатор видачі обов'язковий", nameof(id));

        if (string.IsNullOrWhiteSpace(readerId))
            throw new ArgumentException("Ідентифікатор читача обов'язковий", nameof(readerId));

        if (readerActiveLoans >= 3)
            throw new InvalidOperationException($"Читач {readerId} вже має 3 відкриті видачі. Ліміт вичерпано.");

        if (copy.IsIssued)
            throw new InvalidOperationException($"Примірник {copy.Id} вже виданий, повторна видача неможлива");

        copy.Issue();
        return new Loan(id.Trim(), copy.Id, readerId.Trim(), issuedOn);
    }

    public void Close(DateTime returnedOn, BookCopy copy)
    {
        Status = Status switch
        {
            LoanStatus.Returned => throw new InvalidOperationException($"Видача {Id} вже закрита"),
            LoanStatus.Lost => throw new InvalidOperationException($"Видача {Id} позначена як втрачена. Звичайне повернення неможливе"),
            LoanStatus.Active => LoanStatus.Returned,
            _ => throw new InvalidOperationException("Невідомий стан")
        };

        if (returnedOn < IssuedOn)
            throw new ArgumentOutOfRangeException(nameof(returnedOn), returnedOn, "Дата повернення не може бути раніше дати видачі");

        ReturnedOn = returnedOn;
        copy.Return();
    }

    public LoanDto ToDto() => new(Id, CopyId, ReaderId, IssuedOn, ReturnedOn);

    public override string ToString() =>
        $"Видача {Id}: Примірник {CopyId} -> Читач {ReaderId} - Статус: {Status}";
}