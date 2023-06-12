using System.Text.Json;
using Confluent.Kafka;

namespace video_frames_common;
public class VideoFrameStatsDeserializer : IDeserializer<VideoFrameStats>
{
    VideoFrameStats IDeserializer<VideoFrameStats>.Deserialize(ReadOnlySpan<byte> data, bool isNull, SerializationContext context)
    {
        return JsonSerializer.Deserialize<VideoFrameStats>(data);
    }
}