using Microsoft.Extensions.Caching.Hybrid;
using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Grpc.Service.Json;

public static class HybridJsonOptions
{
    public static readonly JsonSerializerOptions Options = new()
    {
        ReferenceHandler = ReferenceHandler.Preserve
    };
}

public class HybridJsonCacheSerializer<T> : IHybridCacheSerializer<T>
{
    public T Deserialize(ReadOnlySequence<byte> source)
    {
        if (source.IsSingleSegment)
        {
            var value = JsonSerializer.Deserialize<T>(source.FirstSpan, HybridJsonOptions.Options);
            return value!;
        }
        else
        {
            byte[] buffer = source.ToArray();
            var value = JsonSerializer.Deserialize<T>(buffer, HybridJsonOptions.Options);
            return value!;
        }
    }

    public void Serialize(T value, IBufferWriter<byte> target)
    {
        using var writer = new Utf8JsonWriter(target);
        JsonSerializer.Serialize(writer, value, HybridJsonOptions.Options);
        writer.Flush();
    }
}

public class HybridJsonSerializerFactory : IHybridCacheSerializerFactory
{
    public bool TryCreateSerializer<T>([NotNullWhen(true)] out IHybridCacheSerializer<T>? serializer)
    {
        serializer = new HybridJsonCacheSerializer<T>();
        return true;
    }
}
