using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("D:\\logs\\WorkerLog-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

IHost host = Host.CreateDefaultBuilder(args)
    .UseSerilog() // <-- Use Serilog instead of default logging
    .ConfigureServices(services =>
    {
        services.AddHostedService<Worker>(); // Register your Worker class
    })
    .Build();

await host.RunAsync();
