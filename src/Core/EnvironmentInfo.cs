using System.Runtime.InteropServices;
namespace Core;

public sealed record EnvironmentReport(
    string OsDescription,
    string OsVersion,
    string ProcessArchitecture,
    string ClrVersion,
    string FrameworkDescription,
    string BaseDirectory,
    string CurrentDirectory);

public static class EnvironmentInfo
{
    public static EnvironmentReport Collect() => new(
        RuntimeInformation.OSDescription,
        Environment.OSVersion.ToString(),
        RuntimeInformation.ProcessArchitecture.ToString(),
        Environment.Version.ToString(),
        RuntimeInformation.FrameworkDescription,
        AppContext.BaseDirectory,
        Environment.CurrentDirectory);
}