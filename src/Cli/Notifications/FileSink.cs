namespace Cli.Notifications;

using System;
using System.IO;
using Core.Abstractions;

public sealed class FileSink(string filePath) : INotificationSink
{
    private readonly string _filePath = filePath;

    public void Notify(string message)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_filePath)!);
        File.AppendAllText(_filePath, $"[FileSink {DateTime.Now:HH:mm:ss}] {message}{Environment.NewLine}");
    }
}
