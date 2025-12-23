using DotNetEnv;
using Grpc.Net.Client;
using Grpc.Service.Client.Testing;
using Grpc.Service.Client.Tests;
using Grpc.Service.Services;
using Microsoft.Extensions.Configuration;

namespace Grpc.Service.Client;

internal class Program
{
    static async Task<int> Main(string[] args)
    {
        Env.Load();

        var builder = new ConfigurationBuilder();
        builder.SetBasePath(Directory.GetCurrentDirectory())
            .AddEnvironmentVariables();

        var configuration = builder.Build();
        var server = Environment.GetEnvironmentVariable("GRPC_SERVER");
        var jobAdminApiKey = Environment.GetEnvironmentVariable("JOB_ADMIN_API_KEY");
        var jobAdminApiSecret = Environment.GetEnvironmentVariable("JOB_ADMIN_API_SECRET");
        var jobReaderApiKey = Environment.GetEnvironmentVariable("JOB_READER_API_KEY");
        var jobReaderApiSecret = Environment.GetEnvironmentVariable("JOB_READER_API_SECRET");
        var jobWriterApiKey = Environment.GetEnvironmentVariable("JOB_WRITER_API_KEY");
        var jobWriterApiSecret = Environment.GetEnvironmentVariable("JOB_WRITER_API_SECRET");

        if (string.IsNullOrEmpty(server) || string.IsNullOrEmpty(jobAdminApiKey) || string.IsNullOrEmpty(jobAdminApiSecret)
            || string.IsNullOrEmpty(jobReaderApiKey) || string.IsNullOrEmpty(jobReaderApiSecret)
            || string.IsNullOrEmpty(jobWriterApiKey) || string.IsNullOrEmpty(jobWriterApiSecret))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Error: Missing required environment variables");
            Console.WriteLine("Required: GRPC_SERVER, JOB_ADMIN_API_KEY, JOB_ADMIN_API_SECRET, JOB_READER_API_KEY, JOB_READER_API_SECRET, JOB_WRITER_API_KEY, JOB_WRITER_API_SECRET");
            Console.ResetColor();
            return 1;
        }

        Console.WriteLine("═══════════════════════════════════════════════════════");
        Console.WriteLine($"gRPC Service Client Test Runner");
        Console.WriteLine($"Server: {server}");
        Console.WriteLine($"Parallel Execution: Enabled (max 4 concurrent tests)");
        Console.WriteLine("═══════════════════════════════════════════════════════");
        Console.WriteLine();

        var httpClient = new HttpClient();
        using var channel = GrpcChannel.ForAddress($"https://{server}");
        var grpcClient = new Jobs.JobsClient(channel);

        var context = new TestContext
        {
            Server = server,
            JobAdminApiKey = jobAdminApiKey,
            JobAdminApiSecret = jobAdminApiSecret,
            JobReaderApiKey = jobReaderApiKey,
            JobReaderApiSecret = jobReaderApiSecret,
            JobWriterApiKey = jobWriterApiKey,
            JobWriterApiSecret = jobWriterApiSecret,
            HttpClient = httpClient,
            GrpcClient = grpcClient
        };

        var runner = new TestRunner(maxDegreeOfParallelism: 4);
        var testFixture = new JobServiceTests();

        await runner.DiscoverAndRunTestsAsync(testFixture, context);

        runner.PrintSummary();
        runner.ExportJUnitXml("test-results.xml");

        return runner.GetExitCode();
    }
}