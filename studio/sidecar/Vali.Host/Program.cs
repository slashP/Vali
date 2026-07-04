using System.Collections.Concurrent;
using System.Text;
using Vali.Host;

// Protocol lines go to the real stdout; everything Vali.Core prints via Console/AnsiConsole is
// redirected to the capture buffer so validators' messages become structured errors instead of
// corrupting the protocol stream. ValiLogger.Initialize() is deliberately never called (no telemetry).
var stdout = new StreamWriter(Console.OpenStandardOutput(), new UTF8Encoding(false)) { AutoFlush = true };
var writer = new ProtocolWriter(stdout);
var capture = new CaptureTextWriter();
Console.SetOut(capture);

var handlers = new Handlers(writer, capture);
var generateRunner = new GenerateRunner(writer, capture);
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
        case "cancel":
            generateRunner.Cancel(request);
            break;
        default:
            writer.RespondError(request.Id, $"Unknown command '{request.Cmd}'.");
            break;
    }
}
