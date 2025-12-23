using Grpc.Core;
using Grpc.Service.Client.Testing;
using Grpc.Service.Services;

namespace Grpc.Service.Client.Tests;

[TestFixture]
public class JobServiceTests
{
    [Setup]
    public async Task Setup(TestContext context)
    {
        // Ensure test job exists
        await TestHelpers.CreateTestJobAsync(context.Server, context.HttpClient);
    }

    [Teardown]
    public async Task Teardown(TestContext context)
    {
        // Cleanup can be added here if needed
        await Task.CompletedTask;
    }

    [Test(Category = "Authentication")]
    public async Task GetAuthenticationHeaders_DeleteJob_JobAdmin_ReturnsValidHeaders(TestContext context)
    {
        var headers = await TestHelpers.GetAuthenticationAsync("DeleteJob", "JobAdmin", context.Server, context.HttpClient);

        Assert.NotNull(headers, "Headers should not be null");
        Assert.NotEmpty(headers.TimeStamp, "Timestamp should not be empty");
        Assert.NotEmpty(headers.Signature, "Signature should not be empty");
    }

    [Test(Category = "Authentication")]
    public async Task GetAuthenticationHeaders_CreateJob_JobWriter_ReturnsValidHeaders(TestContext context)
    {
        var headers = await TestHelpers.GetAuthenticationAsync("CreateJob", "JobWriter", context.Server, context.HttpClient);

        Assert.NotNull(headers);
        Assert.NotEmpty(headers.TimeStamp);
        Assert.NotEmpty(headers.Signature);
    }

    [Test(Category = "Authentication")]
    public async Task GetAuthenticationHeaders_GetJobs_JobReader_ReturnsValidHeaders(TestContext context)
    {
        var headers = await TestHelpers.GetAuthenticationAsync("GetJobs", "JobReader", context.Server, context.HttpClient);

        Assert.NotNull(headers);
        Assert.NotEmpty(headers.TimeStamp);
        Assert.NotEmpty(headers.Signature);
    }

    [Test(Category = "CRUD")]
    public async Task CreateJob_WithValidCredentials_ReturnsSuccess(TestContext context)
    {
        var headers = await TestHelpers.GetAuthenticationAsync("CreateJob", "JobWriter", context.Server, context.HttpClient);
        var request = new JobCreateRequest
        {
            JobName = $"Test Job {Guid.NewGuid()}",
            JobDescription = "Integration test job"
        };

        var metadata = TestHelpers.CreateMetadata(context.JobWriterApiKey, context.JobWriterApiSecret, headers);
        var response = await context.GrpcClient.CreateJobAsync(request, metadata);

        Assert.NotNull(response);
        Assert.NotEmpty(response.JobId);
        Assert.Equal(request.JobName, response.JobName);

        // Store created job ID for cleanup if needed
        context.Data["CreatedJobId"] = response.JobId;
    }

    [Test(Category = "CRUD")]
    public async Task GetJobs_WithValidCredentials_ReturnsJobs(TestContext context)
    {
        var headers = await TestHelpers.GetAuthenticationAsync("GetJobs", "JobReader", context.Server, context.HttpClient);
        var metadata = TestHelpers.CreateMetadata(context.JobReaderApiKey, context.JobReaderApiSecret, headers);

        var call = context.GrpcClient.GetJobs(new JobListOptions { Limit = 5 }, metadata);
        var jobs = new List<JobResponse>();

        await foreach (var job in call.ResponseStream.ReadAllAsync())
        {
            jobs.Add(job);
        }

        Assert.True(jobs.Count > 0, "Should return at least one job");
        Assert.True(jobs.Count <= 5, "Should not exceed limit");
    }

    [Test(Category = "CRUD")]
    public async Task GetJob_WithValidId_ReturnsJob(TestContext context)
    {
        var headers = await TestHelpers.GetAuthenticationAsync("GetJob", "JobReader", context.Server, context.HttpClient);
        var metadata = TestHelpers.CreateMetadata(context.JobReaderApiKey, context.JobReaderApiSecret, headers);

        var response = await context.GrpcClient.GetJobAsync(
            new JobRequest { JobId = "11111111-1111-1111-1111-111111111111" },
            metadata);

        Assert.NotNull(response);
        Assert.NotEmpty(response.JobId);
        Assert.NotEmpty(response.JobName);
    }

    [Test(Category = "CRUD")]
    public async Task GetJob_WithInvalidId_ThrowsNotFound(TestContext context)
    {
        var headers = await TestHelpers.GetAuthenticationAsync("GetJob", "JobReader", context.Server, context.HttpClient);
        var metadata = TestHelpers.CreateMetadata(context.JobReaderApiKey, context.JobReaderApiSecret, headers);

        await Assert.ThrowsAsync<Grpc.Core.RpcException>(async () =>
        {
            await context.GrpcClient.GetJobAsync(
                new JobRequest { JobId = "00000000-0000-0000-0000-000000000000" },
                metadata);
        }, "Should throw RpcException for invalid job ID");
    }

    [Test(Category = "CRUD")]
    public async Task DeleteJob_WithValidCredentials_ReturnsSuccess(TestContext context)
    {
        // Ensure test job exists
        await TestHelpers.CreateTestJobAsync(context.Server, context.HttpClient);

        var headers = await TestHelpers.GetAuthenticationAsync("DeleteJob", "JobAdmin", context.Server, context.HttpClient);
        var metadata = TestHelpers.CreateMetadata(context.JobAdminApiKey, context.JobAdminApiSecret, headers);

        var response = await context.GrpcClient.DeleteJobAsync(
            new JobRequest { JobId = "11111111-1111-1111-1111-111111111111" },
            metadata);

        Assert.NotNull(response);
    }

    [Test(Category = "CRUD")]
    public async Task DeleteJob_WithInvalidId_ThrowsNotFound(TestContext context)
    {
        var headers = await TestHelpers.GetAuthenticationAsync("DeleteJob", "JobAdmin", context.Server, context.HttpClient);
        var metadata = TestHelpers.CreateMetadata(context.JobAdminApiKey, context.JobAdminApiSecret, headers);

        await Assert.ThrowsAsync<Grpc.Core.RpcException>(async () =>
        {
            await context.GrpcClient.DeleteJobAsync(
                new JobRequest { JobId = "00000000-0000-0000-0000-000000000000" },
                metadata);
        });
    }

    [Test(Category = "Authorization")]
    public async Task CreateJob_WithReaderCredentials_ThrowsPermissionDenied(TestContext context)
    {
        var headers = await TestHelpers.GetAuthenticationAsync("CreateJob", "JobReader", context.Server, context.HttpClient);
        var metadata = TestHelpers.CreateMetadata(context.JobReaderApiKey, context.JobReaderApiSecret, headers);

        await Assert.ThrowsAsync<Grpc.Core.RpcException>(async () =>
        {
            await context.GrpcClient.CreateJobAsync(
                new JobCreateRequest { JobName = "Test", JobDescription = "Test" },
                metadata);
        });
    }

    [Test(Category = "Authorization")]
    public async Task DeleteJob_WithReaderCredentials_ThrowsPermissionDenied(TestContext context)
    {
        var headers = await TestHelpers.GetAuthenticationAsync("DeleteJob", "JobReader", context.Server, context.HttpClient);
        var metadata = TestHelpers.CreateMetadata(context.JobReaderApiKey, context.JobReaderApiSecret, headers);

        await Assert.ThrowsAsync<Grpc.Core.RpcException>(async () =>
        {
            await context.GrpcClient.DeleteJobAsync(
                new JobRequest { JobId = "11111111-1111-1111-1111-111111111111" },
                metadata);
        });
    }

    [Test(Category = "Authorization")]
    public async Task GetJobs_WithWriterCredentials_ThrowsPermissionDenied(TestContext context)
    {
        var headers = await TestHelpers.GetAuthenticationAsync("GetJobs", "JobWriter", context.Server, context.HttpClient);
        var metadata = TestHelpers.CreateMetadata(context.JobWriterApiKey, context.JobWriterApiSecret, headers);

        await Assert.ThrowsAsync<Grpc.Core.RpcException>(async () =>
        {
            var call = context.GrpcClient.GetJobs(new JobListOptions { Limit = 5 }, metadata);
            await foreach (var _ in call.ResponseStream.ReadAllAsync())
            {
                // Should throw before reading any items
            }
        });
    }

    [Test(Category = "Validation")]
    public async Task CreateJob_WithEmptyName_ThrowsInvalidArgument(TestContext context)
    {
        var headers = await TestHelpers.GetAuthenticationAsync("CreateJob", "JobWriter", context.Server, context.HttpClient);
        var metadata = TestHelpers.CreateMetadata(context.JobWriterApiKey, context.JobWriterApiSecret, headers);

        await Assert.ThrowsAsync<Grpc.Core.RpcException>(async () =>
        {
            await context.GrpcClient.CreateJobAsync(
                new JobCreateRequest { JobName = "", JobDescription = "Test" },
                metadata);
        });
    }

    [Test(Category = "Validation")]
    public async Task CreateJob_WithEmptyDescription_ThrowsInvalidArgument(TestContext context)
    {
        var headers = await TestHelpers.GetAuthenticationAsync("CreateJob", "JobWriter", context.Server, context.HttpClient);
        var metadata = TestHelpers.CreateMetadata(context.JobWriterApiKey, context.JobWriterApiSecret, headers);

        await Assert.ThrowsAsync<Grpc.Core.RpcException>(async () =>
        {
            await context.GrpcClient.CreateJobAsync(
                new JobCreateRequest { JobName = "Test", JobDescription = "" },
                metadata);
        });
    }
}