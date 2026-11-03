namespace Core.Reports;

public sealed record BenchmarkResult(string Variant, int Rows, double Milliseconds);
