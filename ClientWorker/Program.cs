using ClientWorker;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.DependencyInjection;
using ClientWorker.Infrastructure.Resilience;


var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<Worker>();
builder.Services.AddHttpClient("MyHttpClient");
builder.Services.AddLogging();
builder.Services.AddTransient<ICircuitBreaker<string>, CircuitBreaker<string>>(sp =>
{
    return new ClientWorker.Infrastructure.Resilience.CircuitBreaker<string>(
        failureThreshold: 5,
        failureWindow: TimeSpan.FromMinutes(1),
        openDuration: TimeSpan.FromMinutes(5),
        logger: sp.GetRequiredService<ILogger<ICircuitBreaker<string>>>()
    );
});

var host = builder.Build();
host.Run();
