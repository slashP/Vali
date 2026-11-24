using System.Collections.Concurrent;
using System.Text;
using Vali.Host;

// Protocol lines go to the real stdout; everything Vali.Core prints via Console/AnsiConsole is
// redirected to the capture buffer so validators' messages become structured errors instead of
// corrupting the protocol stream. ValiLogger.Initialize() is deliberately never called (no telemetry).
var stdout = new StreamWriter(Console.OpenStandardOutput(), new UTF8Encoding(false)) { AutoFlush = true };
Console.SetIn(new StreamReader(Console.OpenStandardInput(), new UTF8Encoding(false)));
var writer = new ProtocolWriter(stdout);
var capture = new CaptureTextWriter();
Console.SetOut(capture);

// Resolve the download folder before any command reads it, honoring the user's
// CLI-configured folder and only falling back when nothing is set and the
// default isn't writable (see DownloadFolderResolver).
DownloadFolderResolver.EnsureUsableDownloadFolder();

var handlers = new Handlers(writer, capture);
var generateRunner = new GenerateRunner(writer, capture);
var downloadRunner = new DownloadRunner(writer);
var inFlight = new ConcurrentDictionary<Guid, Task>();

string? line;
while ((line = await Console.In.ReadLineAsync()) is not null)
{
    if (string.IsNullOrWhiteSpace(line))
    {
        continue;
    }

    Request request;
    try
    {
        request = Request.Parse(line);
    }
    catch (Exception e)
    {
        writer.RespondError(null, "Unparseable request line.", e.Message);
        continue;
    }

    var key = Guid.NewGuid();
    var task = Task.Run(async () =>
    {
        try
        {
            await Dispatch(request);
        }
        catch (Exception e)
        {
            writer.RespondError(request.Id, e.Message, e.ToString());
        }
    });
    inFlight[key] = task;
    _ = task.ContinueWith(_ => inFlight.TryRemove(key, out Task? _), TaskScheduler.Default);
}

// stdin closed — finish outstanding work (a running generation included) before exiting.
await Task.WhenAll(inFlight.Values.ToArray());
return 0;

async Task Dispatch(Request request)
{
    switch (request.Cmd)
    {
        case "ping":
            handlers.Ping(request);
            break;
        case "validate":
            await handlers.Validate(request);
            break;
        case "expressionCheck":
            handlers.ExpressionCheck(request);
            break;
        case "effectiveDefinition":
            await handlers.EffectiveDefinition(request);
            break;
        case "countries":
            handlers.Countries(request);
            break;
        case "subdivisions":
            handlers.Subdivisions(request);
            break;
        case "dataStatus":
            handlers.DataStatus(request);
            break;
        case "generate":
            await generateRunner.Run(request);
            break;
        case "download":
            await downloadRunner.Run(request);
            break;
        case "remoteStatus":
            await downloadRunner.RemoteStatus(request);
            break;
        case "valiInfo":
            await downloadRunner.ValiInfo(request);
            break;
        case "cancel":
        {
            var targetId = request.GetString("targetId");
            if (generateRunner.TryCancel(targetId) || downloadRunner.TryCancel(targetId))
            {
                writer.Respond(request.Id, new { cancelled = true });
            }
            else
            {
                writer.RespondError(request.Id, "No running operation with that id.");
            }

            break;
        }
        default:
            writer.RespondError(request.Id, $"Unknown command '{request.Cmd}'.");
            break;
    }
}
