using Azure.Monitor.OpenTelemetry.Exporter;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Vali.Core;

public static class ValiLogger
{
    public static ILoggerFactory Factory = new NullLoggerFactory();

    public static ILoggerFactory Initialize()
    {
        try
        {
            Factory = LoggerFactory.Create(builder =>
            {
                builder.AddOpenTelemetry(logging =>
                {
                    logging.AddAzureMonitorLogExporter(options => options.ConnectionString = "InstrumentationKey=53b3bb0f-dea2-4de2-bb67-0b73809ad986;IngestionEndpoint=https://northeurope-2.in.applicationinsights.azure.com/;LiveEndpoint=https://northeurope.livediagnostics.monitor.azure.com/;ApplicationId=26392cf2-ac74-4967-b05f-846efd94f6c2");
                });
            });
        }
        catch (Exception)
        {
            Console.WriteLine("Tracing/logging is disabled.");
        }

        return Factory;
    }
}