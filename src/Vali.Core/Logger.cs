using Spectre.Console;

namespace Vali.Core;

public static class ConsoleLogger
{
    public static bool Silent = false;

    public static void Success(string message) => Log(message, "green");

    public static void Info(string message) => Log(message, "blue");

    public static void Warn(string message) => Log(message, "darkorange3");

    public static void Error(string message) => Log(message, "red");

    private static string Escape(this string message) =>
        message.Replace("[", "[[").Replace("]", "]]");

    private static void Log(string message, string color)
    {
        if (Silent)
        {
            return;
        }

        AnsiConsole.MarkupLine($"[{color}]{message.Escape()}[/]");
    }
}