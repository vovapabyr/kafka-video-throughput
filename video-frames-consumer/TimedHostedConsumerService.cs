
using Confluent.Kafka;

public class TimedHostedService : IHostedService, IDisposable
{
    private int executionCount = 0;
    private readonly ILogger<TimedHostedService> _logger;
    private readonly IConfiguration _configuration;
    private Timer? _timer = null;
    private IConsumer<Ignore, string> _consumer;

    public TimedHostedService(ILogger<TimedHostedService> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }

    public Task StartAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Timed Hosted Service running.");

        var config = new ConsumerConfig
        {
            BootstrapServers = _configuration["Kafka:BootstrapServers"],
            GroupId = "video-frames",
            AutoOffsetReset = AutoOffsetReset.Earliest
        };

        _consumer = new ConsumerBuilder<Ignore, string>(config).Build();
        _consumer.Subscribe(_configuration["Kafka:Topic"]);
        _timer = new Timer(Consume, stoppingToken, TimeSpan.Zero, TimeSpan.FromSeconds(1));
        

        return Task.CompletedTask;
    }

    private void Consume(object? cancellationToken)
    {
        var consumeResult = _consumer.Consume((CancellationToken)cancellationToken);

        _logger.LogInformation("Message: '{Value}'. Timestamp: '{Timestamp}'.", consumeResult.Message.Value, consumeResult.Message.Timestamp.UtcDateTime);
    }

    public Task StopAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Timed Hosted Service is stopping.");

        _timer?.Change(Timeout.Infinite, 0);

        _consumer.Close();
        
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _timer?.Dispose();

        _consumer.Dispose();
    }
}