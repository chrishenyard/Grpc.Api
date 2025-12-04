namespace Grpc.Service.Settings;

public class SeqSettings
{
    public const string Section = "SeqSettings";

    public required string ServerUrl { get; set; }
    public required string ApiKey { get; set; }
}
