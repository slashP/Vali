using Microsoft.Extensions.Logging;

namespace Vali.Core;

public static partial class LoggerExtensions
{
    [LoggerMessage(LogLevel.Information, "Map generated with `{locationCount}` locations in `{timeSpan}`.")]
    public static partial void MapGenerated(this ILogger logger, int locationCount, TimeSpan timeSpan);

    [LoggerMessage(LogLevel.Information, "Downloaded files for `{countryCode}` in `{timeSpan}`. Exit requested: `{exitRequested}`")]
    public static partial void FilesDownloaded(this ILogger logger, string? countryCode, TimeSpan timeSpan, bool exitRequested);
}
