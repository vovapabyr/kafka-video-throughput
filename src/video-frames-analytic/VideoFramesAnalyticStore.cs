using System.Collections.Concurrent;
using System.Data;
using video_frames_common;

namespace video_frames_analytic;

public class VideoFramesAnalyticInMemoryStore 
{
    // Timestamp - bit per second 
    private readonly Dictionary<long, double> _throughput = new Dictionary<long, double>();

    private readonly Dictionary<int, double> _latency = new Dictionary<int, double>();

    private double _currentLatency; 

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

        _currentLatency = (videoFrameStats.EndTicks - videoFrameStats.StartTicks) / (double)TimeSpan.TicksPerMillisecond;
        _latency.TryAdd(videoFrameStats.Index, _currentLatency);            
    }

    public double GetAvgThroughput() => _throughput.Any() ? _throughput.Values.Average() : 0;

    public double GetLatency() => _currentLatency;

    public DataTable GetThroughputAsDatatable() 
    {
        DataTable throughputTable = new DataTable();

        throughputTable.Columns.Add("Time", typeof(TimeSpan));
        throughputTable.Columns.Add("Throughput (Mbps)", typeof(double));

        foreach (var item in _throughput.OrderBy(e => e.Key))
        {
            var date = new DateTime(item.Key);
            throughputTable.Rows.Add(date.TimeOfDay, item.Value);
        }

        return throughputTable;
    }

    public DataTable GetLatencyAsDatatable() 
    {
        DataTable latencyTable = new DataTable();

        latencyTable.Columns.Add("Frames#", typeof(int));
        latencyTable.Columns.Add("Latency (ms)", typeof(double));

        foreach (var item in _latency.OrderBy(e => e.Key))
        {
            latencyTable.Rows.Add(item.Key, item.Value);
        }

        return latencyTable;
    }
}