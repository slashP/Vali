using Spectre.Console;

namespace Vali.Core;

public static class ConsoleLogger
{
    public static void Success(string message) => AnsiConsole.MarkupLine($"[green]{message.Escape()}[/]");
    public static void Info(string message) => AnsiConsole.MarkupLine($"[blue]{message.Escape()}[/]");
    public static void Warn(string message) => AnsiConsole.MarkupLine($"[darkorange3]{message.Escape()}[/]");
    public static void Error(string message) => AnsiConsole.MarkupLine($"[red]{message.Escape()}[/]");

    private static string Escape(this string message) =>
        message.Replace("[", "[[").Replace("]", "]]");
}