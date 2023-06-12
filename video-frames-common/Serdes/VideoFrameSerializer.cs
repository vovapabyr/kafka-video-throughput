using System.Text.Json;
using Confluent.Kafka;

namespace video_frames_common;
public class VideoFrameSerializer : ISerializer<VideoFrame>
{
    public byte[] Serialize(VideoFrame data, SerializationContext context)
    {
        return JsonSerializer.SerializeToUtf8Bytes(data);
    }
}