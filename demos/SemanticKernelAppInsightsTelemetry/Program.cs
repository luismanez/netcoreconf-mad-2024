using Azure.Monitor.OpenTelemetry.Exporter;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using OpenTelemetry;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace SemanticKernelAppInsightsTelemetry
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("hostsettings.json", optional: false, reloadOnChange: true)
            .Build();

            var connectionString = configuration["ApplicationInsights:ConnectionString"]!;

            var resourceBuilder = ResourceBuilder
                .CreateDefault()
                .AddService("TelemetryApplicationInsightsQuickstart");

            // Enable model diagnostics with sensitive data.
            AppContext.SetSwitch("Microsoft.SemanticKernel.Experimental.GenAI.EnableOTelDiagnosticsSensitive", true);

            using var traceProvider = Sdk.CreateTracerProviderBuilder()
                .SetResourceBuilder(resourceBuilder)
                .AddSource("Microsoft.SemanticKernel*")
                .AddAzureMonitorTraceExporter(options => options.ConnectionString = connectionString)
                .Build();

            using var meterProvider = Sdk.CreateMeterProviderBuilder()
                .SetResourceBuilder(resourceBuilder)
                .AddMeter("Microsoft.SemanticKernel*")
                .AddAzureMonitorMetricExporter(options => options.ConnectionString = connectionString)
                .Build();

            using var loggerFactory = LoggerFactory.Create(builder =>
            {
                // Add OpenTelemetry as a logging provider
                builder.AddOpenTelemetry(options =>
                {
                    options.SetResourceBuilder(resourceBuilder);
                    options.AddAzureMonitorLogExporter(options => options.ConnectionString = connectionString);
                    // Format log messages. This is default to false.
                    options.IncludeFormattedMessage = true;
                    options.IncludeScopes = true;
                });
                builder.SetMinimumLevel(LogLevel.Information);
            });

            IKernelBuilder builder = Kernel.CreateBuilder();

            builder.Services.AddSingleton(loggerFactory);

            builder.AddAzureOpenAIChatCompletion(
                deploymentName: configuration["AzureOpenAI:DeploymentName"]!,
                endpoint: configuration["AzureOpenAI:Endpoint"]!,
                apiKey: configuration["AzureOpenAI:ApiKey"]!
            );

            Kernel kernel = builder.Build();

            var answer = await kernel.InvokePromptAsync(
                "Tell me a joke"
            );

            Console.WriteLine(answer);
        }
    }
}