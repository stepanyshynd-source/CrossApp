using System.Runtime.InteropServices;
namespace Core;

public sealed record EnvironmentReport(
    string OsDescription,
    string OsVersion,
    string ProcessArchitecture,
    string ClrVersion,
    string FrameworkDescription,
    string BaseDirectory,
    string CurrentDirectory,
    string BuildNote);

public static class EnvironmentInfo
{
    public static EnvironmentReport Collect()
    {
#if NET10_0_OR_GREATER
        const string BuildNote = "збірка під net10.0";
#else
        const string BuildNote = "збірка під net8.0";
#endif

        return new(
            RuntimeInformation.OSDescription,
            Environment.OSVersion.ToString(),
            RuntimeInformation.ProcessArchitecture.ToString(),
            Environment.Version.ToString(),
            RuntimeInformation.FrameworkDescription,
            AppContext.BaseDirectory,
            Environment.CurrentDirectory,
            BuildNote);
    }
}