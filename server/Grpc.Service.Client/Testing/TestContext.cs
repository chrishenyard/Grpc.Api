using Grpc.Service.Services;

namespace Grpc.Service.Client.Testing;

public class TestContext
{
    public required string Server { get; init; }
    public required string JobAdminApiKey { get; init; }
    public required string JobAdminApiSecret { get; init; }
    public required string JobReaderApiKey { get; init; }
    public required string JobReaderApiSecret { get; init; }
    public required string JobWriterApiKey { get; init; }
    public required string JobWriterApiSecret { get; init; }
    public required HttpClient HttpClient { get; init; }
    public required Jobs.JobsClient GrpcClient { get; init; }
    public Dictionary<string, object> Data { get; } = new();
}

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public class TestAttribute : Attribute
{
    public string? Category { get; set; }
    public bool Skip { get; set; }
    public string? SkipReason { get; set; }
}

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class TestFixtureAttribute : Attribute { }

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public class SetupAttribute : Attribute { }

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public class TeardownAttribute : Attribute { }