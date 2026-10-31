namespace Cli.Notifications;

using System;
using Core.Abstractions;

public sealed class ConsoleSink : INotificationSink
{
    public void Notify(string message) =>
        Console.WriteLine($"[ConsoleSink] {message}");
}
