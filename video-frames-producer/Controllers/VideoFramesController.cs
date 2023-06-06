using Confluent.Kafka;
using Microsoft.AspNetCore.Mvc;

namespace video_frames_generator.Controllers;

[ApiController]
[Route("[controller]")]
public class VideoController : ControllerBase
{
    private readonly ILogger<VideoController> _logger;
    private readonly IConfiguration _configuration;

    public VideoController(ILogger<VideoController> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }

    [HttpPost(template:"generate", Name = "GenerateFrames")]
    public async Task GenerateFrames()
    {
        var config = new ProducerConfig()
        {
            BootstrapServers = _configuration["Kafka:BootstrapServers"]
        };
        using (var producer = new ProducerBuilder<Null, string>(config).Build())
        {
            var result = await producer.ProduceAsync(_configuration["Kafka:Topic"], new Message<Null, string> { Value="a log message" });
        }
    }
}
