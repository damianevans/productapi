namespace ClientWorker;
using ClientWorker.Infrastructure.Resilience;
using Microsoft.Extensions.Http;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ICircuitBreaker<string> _circuitBreaker;

    public Worker(ILogger<Worker> logger, 
                    IHttpClientFactory httpClientFactory, 
                    ICircuitBreaker<string> circuitBreaker)
    {
        _circuitBreaker = circuitBreaker;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var client = _httpClientFactory.CreateClient("MyHttpClient");
        while (!stoppingToken.IsCancellationRequested)
        {
            if (_logger.IsEnabled(LogLevel.Information))
            {
                _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
            }

            await _circuitBreaker.ExecuteAsync(async () =>
            {
                var response = await client.GetAsync("https://example.com");
                response.EnsureSuccessStatusCode();
                var content = await response.Content.ReadAsStringAsync();
                return content;
            });

            await Task.Delay(5000, stoppingToken);
        }
    }
}
