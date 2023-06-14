using Confluent.Kafka;
using video_frames_common;

namespace video_frames_analytic;
public class VideoFramesAnalyticConsumerService : BackgroundService
{
    private readonly ILogger<VideoFramesAnalyticConsumerService> _logger;
    private readonly VideoFramesAnalyticInMemoryStore _analyticStore;
    private readonly string _framesAnalyticTopicName;
    private IConsumer<Ignore, VideoFrameStats> _kafkaConsumer;

    public VideoFramesAnalyticConsumerService(ILogger<VideoFramesAnalyticConsumerService> logger, IConfiguration configuration, VideoFramesAnalyticInMemoryStore analyticStore)
    {
        _logger = logger;
        _analyticStore = analyticStore;
        _framesAnalyticTopicName = configuration["Kafka:FramesAnalyticTopicName"];
        var consumerConfig = new ConsumerConfig
        {
            BootstrapServers = configuration["Kafka:BootstrapServers"],
            GroupId = "video-frames-analytic",
            AutoOffsetReset = AutoOffsetReset.Earliest
        };
        _kafkaConsumer = new ConsumerBuilder<Ignore, VideoFrameStats>(consumerConfig).SetValueDeserializer(new VideoFrameStatsDeserializer()).Build();
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
        _kafkaConsumer.Subscribe(_framesAnalyticTopicName);

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var cr = _kafkaConsumer.Consume(cancellationToken);
                _logger.LogInformation("Index: '{Index}'. Size(bytes): '{Size}'. Start: '{Start}'. End: '{End}'.", cr.Message.Value.Index, cr.Message.Value.Size, cr.Message.Value.StartTicks, cr.Message.Value.EndTicks);
                _analyticStore.AddFrameStats(cr.Message.Value);
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