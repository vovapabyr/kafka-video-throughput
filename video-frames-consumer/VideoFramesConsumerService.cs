
using Confluent.Kafka;
using video_frames_common;

public class VideoFramesConsumerService : BackgroundService
{
    private readonly ILogger<VideoFramesConsumerService> _logger;
    private readonly string _topic;
    private IConsumer<Ignore, VideoFrame> _kafkaConsumer;

    public VideoFramesConsumerService(ILogger<VideoFramesConsumerService> logger, IConfiguration configuration)
    {
        _logger = logger;
        _topic = configuration["Kafka:Topic"];
        var config = new ConsumerConfig
        {
            BootstrapServers = configuration["Kafka:BootstrapServers"],
            GroupId = "video-frames",
            AutoOffsetReset = AutoOffsetReset.Earliest,
        };
        _kafkaConsumer = new ConsumerBuilder<Ignore, VideoFrame>(config).SetValueDeserializer(new VideoFrameDeserializer()).Build();
    }

    public override void Dispose()
    {
        _kafkaConsumer.Close();
        _kafkaConsumer.Dispose();

        base.Dispose();
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        return Task.Run(() => StartConsumerLoop(stoppingToken), stoppingToken);
    }

    private void StartConsumerLoop(CancellationToken cancellationToken)
    {
        _kafkaConsumer.Subscribe(_topic);

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var cr = _kafkaConsumer.Consume(cancellationToken);
                Thread.Sleep(100);
                _logger.LogInformation("Index: '{Index}'. Size(bytes): '{Size}'. Partition: '{Partition}'. Timestamp: '{Timestamp}'.", cr.Message.Value.Index, Convert.FromBase64String(cr.Message.Value.FrameBase64).Length, cr.Partition.Value, cr.Message.Timestamp.UtcDateTime);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (ConsumeException e)
            {
                // Consumer errors should generally be ignored (or logged) unless fatal.
                _logger.LogWarning($"Consume error: {e.Error.Reason}");

                if (e.Error.IsFatal)
                {
                    // https://github.com/edenhill/librdkafka/blob/master/INTRODUCTION.md#fatal-consumer-errors
                    break;
                }
            }
            catch (Exception e)
            {
                _logger.LogError($"Unexpected error: {e}");
                break;
            }
        }
    }
}