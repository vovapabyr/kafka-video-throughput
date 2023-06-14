using System.Text.Json;
using Confluent.Kafka;

namespace video_frames_common;
public class VideoFrameDeserializer : IDeserializer<VideoFrame>
{
    public VideoFrame Deserialize(ReadOnlySpan<byte> data, bool isNull, SerializationContext context)
    {
        return JsonSerializer.Deserialize<VideoFrame>(data);
    }
}