namespace Core.Reports;

public sealed record ReaderActivity(string ReaderId, string FullName, int TotalLoans, int ActiveLoans);
