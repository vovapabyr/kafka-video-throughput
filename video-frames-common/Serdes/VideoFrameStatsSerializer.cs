using System.Text.Json;
using Confluent.Kafka;

namespace video_frames_common;
public class VideoFrameStatsSerializer : ISerializer<VideoFrameStats>
{
    public byte[] Serialize(VideoFrameStats data, SerializationContext context)
    {
        return JsonSerializer.SerializeToUtf8Bytes(data);
    }
}