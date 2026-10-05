namespace Core.Domain;

using System;
using Core.Dto;

public sealed class Loan
{
    public string Id { get; }
    public string CopyId { get; }
    public string ReaderId { get; }
    public DateTime IssuedOn { get; }

    public DateTime? ReturnedOn { get; private set; }

    public bool IsClosed => ReturnedOn.HasValue;

    private Loan(string id, string copyId, string readerId, DateTime issuedOn, DateTime? returnedOn = null)
    {
        Id = id;
        CopyId = copyId;
        ReaderId = readerId;
        IssuedOn = issuedOn;
        ReturnedOn = returnedOn;
    }

    public static Loan Open(string id, BookCopy copy, string readerId, DateTime issuedOn)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("Ідентифікатор видачі обов'язковий", nameof(id));

        if (string.IsNullOrWhiteSpace(readerId))
            throw new ArgumentException("Ідентифікатор читача обов'язковий", nameof(readerId));

        if (copy.IsIssued)
            throw new InvalidOperationException($"Примірник {copy.Id} вже виданий, повторна видача неможлива");

        copy.Issue();

        return new Loan(id.Trim(), copy.Id, readerId.Trim(), issuedOn);
    }

    public void Close(DateTime returnedOn, BookCopy copy)
    {
        if (IsClosed)
            throw new InvalidOperationException($"Видача {Id} вже закрита");

        if (returnedOn < IssuedOn)
            throw new ArgumentOutOfRangeException(nameof(returnedOn), returnedOn,
                "Дата повернення не може бути раніше дати видачі");

        ReturnedOn = returnedOn;
        copy.Return();
    }

    public LoanDto ToDto() => new(Id, CopyId, ReaderId, IssuedOn, ReturnedOn);

    public static Loan FromDto(LoanDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Id) || string.IsNullOrWhiteSpace(dto.CopyId))
            throw new ArgumentException("Некоректні дані для відновлення Loan");

        return new Loan(dto.Id, dto.CopyId, dto.ReaderId, dto.IssuedOn, dto.ReturnedOn);
    }

    public override string ToString() =>
        $"Видача {Id}: Примірник {CopyId} -> Читач {ReaderId} (Видано: {IssuedOn:yyyy-MM-dd}) - {(IsClosed ? $"Повернуто {ReturnedOn:yyyy-MM-dd}" : "Активна")}";
}