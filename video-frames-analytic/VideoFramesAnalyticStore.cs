using System.Collections.Concurrent;
using video_frames_common;

namespace video_frames_analytic;

public class VideoFramesAnalyticInMemoryStore 
{
    // Timestamp - bit per second 
    private readonly ConcurrentDictionary<long, double> _throughput = new ConcurrentDictionary<long, double>();

    private double _latency; 
    private ILogger<VideoFramesAnalyticInMemoryStore> _logger;

    public VideoFramesAnalyticInMemoryStore(ILogger<VideoFramesAnalyticInMemoryStore> logger)
    {
        _logger = logger;
    }

    public void AddFrameStats(VideoFrameStats videoFrameStats)
    {
        var processedTime = videoFrameStats.EndTicks - (videoFrameStats.EndTicks % TimeSpan.TicksPerSecond);
        var processedFrameSizeInMegaBits = videoFrameStats.Size * 8 / 1_000_000.0;
        if (!_throughput.ContainsKey(processedTime))
            _throughput.TryAdd(processedTime, processedFrameSizeInMegaBits);
        else
            _throughput[processedTime] += processedFrameSizeInMegaBits;

        var processedLatency = (videoFrameStats.EndTicks - videoFrameStats.StartTicks) / (double)TimeSpan.TicksPerSecond;
        Interlocked.Exchange(ref _latency, processedLatency);
    }

    public double GetThroughput() => _throughput.Any() ? _throughput.Values.Average() : 0;

    public double GetLatency() => _latency;
}