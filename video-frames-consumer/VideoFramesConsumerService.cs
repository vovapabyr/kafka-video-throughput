
using Confluent.Kafka;
using video_frames_common;

public class VideoFramesConsumerService : BackgroundService
{
    private readonly ILogger<VideoFramesConsumerService> _logger;
    private readonly string _framesTopicName;
    private readonly string _framesAnalyticTopicName;
    private IConsumer<Ignore, VideoFrame> _kafkaConsumer;
    private IProducer<Null, VideoFrameStats> _kafkaProducer;

    public VideoFramesConsumerService(ILogger<VideoFramesConsumerService> logger, IConfiguration configuration)
    {
        _logger = logger;
        _framesTopicName = configuration["Kafka:FramesTopicName"];
        _framesAnalyticTopicName = configuration["Kafka:FramesAnalyticTopicName"];
        var consumerConfig = new ConsumerConfig
        {
            BootstrapServers = configuration["Kafka:BootstrapServers"],
            GroupId = "video-frames",
            AutoOffsetReset = AutoOffsetReset.Earliest,
        };
        _kafkaConsumer = new ConsumerBuilder<Ignore, VideoFrame>(consumerConfig).SetValueDeserializer(new VideoFrameDeserializer()).Build();
        var producerConfig = new ProducerConfig()
        {
            BootstrapServers = configuration["Kafka:BootstrapServers"]
        };
        _kafkaProducer = new ProducerBuilder<Null, VideoFrameStats>(producerConfig).SetValueSerializer(new VideoFrameStatsSerializer()).Build();
    }

    public override void Dispose()
    {
        _kafkaConsumer.Close();
        _kafkaConsumer.Dispose();
        _kafkaProducer.Dispose();

        base.Dispose();
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        return Task.Run(() => StartConsumerLoop(stoppingToken), stoppingToken);
    }

    private void StartConsumerLoop(CancellationToken cancellationToken)
    {
        _kafkaConsumer.Subscribe(_framesTopicName);

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var cr = _kafkaConsumer.Consume(cancellationToken);
                _kafkaProducer.Produce(_framesAnalyticTopicName, new Message<Null, VideoFrameStats> 
                {  
                    Value = new VideoFrameStats()
                    { 
                        Index = cr.Message.Value.Index,
                        Size = Convert.FromBase64String(cr.Message.Value.FrameBase64).Length,
                        StartTicks = cr.Message.Timestamp.UtcDateTime.Ticks,
                        EndTicks = DateTime.UtcNow.Ticks
                    }
                });
                Thread.Sleep(100);
                _logger.LogInformation("Index: '{Index}'. Size(bytes): '{Size}'. Partition: '{Partition}'. Timestamp: '{Timestamp}'.", cr.Message.Value.Index, Convert.FromBase64String(cr.Message.Value.FrameBase64).Length, cr.Partition.Value, cr.Message.Timestamp.Type);
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
        _kafkaProducer.Flush(cancellationToken);
    }
}