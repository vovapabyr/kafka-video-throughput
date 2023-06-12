
using Confluent.Kafka;
using OpenCvSharp;
using video_frames_common;

public class VideoFramesProducerService : BackgroundService
{
    private readonly ILogger<VideoFramesProducerService> _logger;
    private readonly string _topic;
    private readonly string _videoName;
    private IProducer<Null, VideoFrame> _kafkaProducer;

    public VideoFramesProducerService(ILogger<VideoFramesProducerService> logger, IConfiguration configuration)
    {
        _logger = logger;
        _topic = configuration["Kafka:Topic"];
        _videoName = configuration["VideoName"];
        var config = new ProducerConfig()
        {
            BootstrapServers = configuration["Kafka:BootstrapServers"]
        };
        _kafkaProducer = new ProducerBuilder<Null, VideoFrame>(config).SetValueSerializer(new VideoFrameSerializer()).Build();
    }

    public override void Dispose()
    {
        _kafkaProducer.Dispose();

        base.Dispose();
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        return Task.Run(() => StartFramesProducing(stoppingToken), stoppingToken); 
    }

    private void StartFramesProducing(CancellationToken cancellationToken)
    {
        using (var capture = new VideoCapture($"videos//{ _videoName }"))
        using (Mat image = new Mat()) // Frame image buffer
        {
            _logger.LogInformation($"Start sending video '{ _videoName }' frames. Frames Count: { capture.FrameCount }. FPS: { capture.Fps }");
            var frameIndex = 0;
            // Loop while we can read an image (aka: image.Empty is not true)
            try
            {
                do
                {
                    // Read the next
                    capture.Read(image);
                    _kafkaProducer.Produce(_topic, new Message<Null, VideoFrame> {  Value = new VideoFrame(){ Index = frameIndex, FrameBase64 = Convert.ToBase64String(image.ToBytes()) }});
                    // We only want to save every FPS hit since we have 1 image per second -> mod
                    // if (frameIndex % capture.Fps == 0)
                    // {
                    //     var saveResult = image.SaveImage($"image_{frameIndex}.png");
                    //     Console.WriteLine($"[VideoProcessor] Saved image #{frameIndex}");
                    // }

                    frameIndex++;
                } while (!image.Empty());
            }
            catch (OpenCVException) {}
            catch (ProduceException<Null, VideoFrame> ex)
            {
                _logger.LogWarning($"Failed to process message with index: { ex.DeliveryResult.Value.Index }.");
            }

            _logger.LogInformation($"Finished video processing.");
        }
    }
}
