using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Grpc.Data.Contracts;
using Grpc.Data.Entities;
using Grpc.Service.Attributes;

namespace Grpc.Service.Services;

public class JobService(IJobRepository jobRepository, ILogger<JobService> logger) : Jobs.JobsBase
{
    private readonly IJobRepository _jobRepository = jobRepository;
    private readonly ILogger<JobService> _logger = logger;

    private readonly List<string> _statuses =
        ["Unwilling to accept basement-level pay",
        "You applied? You're kidding.",
        "Please!",
        "AI Hallucinations Resume",
        "You're number 1501 in the applicant pool",
        "You're now number 1499 in the applicant pool",
        "Am I hired? It is decidedly so!"];

    [RequireApiGroup("JobWriter")]
    public override async Task<JobResponse> CreateJob(JobCreateRequest request, ServerCallContext context)
    {
        var jobDto = JobDto.Create(request.JobName, request.JobDescription);
        await _jobRepository.CreateJobAsync(jobDto, context.CancellationToken);

        _logger.LogInformation("Created job with ID {JobId}", jobDto.JobId);

        return new JobResponse
        {
            JobId = jobDto.JobId.ToString(),
            JobName = jobDto.Name,
            JobDescription = jobDto.Description,
            Status = "Job created successfully"
        };
    }

    [RequireApiGroup("JobReader")]
    public override async Task GetJobs(JobListOptions options, IServerStreamWriter<JobResponse> responseStream, ServerCallContext context)
    {
        var jobDtos = await _jobRepository.GetJobsAsync(options.Limit, context.CancellationToken);

        _logger.LogInformation("Retrieved {JobCount} jobs", options.Limit);

        await foreach (var dto in jobDtos)
        {
            var jobResponse = new JobResponse
            {
                JobId = dto.JobId.ToString(),
                JobName = dto.Name,
                JobDescription = dto.Description,
                Status = _statuses[new Random().Next(0, _statuses.Count)]
            };

            await responseStream.WriteAsync(jobResponse, context.CancellationToken);
        }
    }

    [RequireApiGroup("JobReader")]
    public override async Task<JobResponse> GetJob(JobRequest request, ServerCallContext context)
    {
        var jobDto = await _jobRepository.GetJobByIdAsync(Guid.Parse(request.JobId), context.CancellationToken);

        if (jobDto == null)
        {
            _logger.LogInformation("Job with ID {JobId} not found", request.JobId);
            throw new RpcException(new Status(StatusCode.NotFound, $"Job with ID {request.JobId} not found."));
        }

        return new JobResponse
        {
            JobId = jobDto.JobId.ToString(),
            JobName = jobDto.Name,
            JobDescription = jobDto.Description,
            Status = _statuses[new Random().Next(0, _statuses.Count)]
        };
    }

    [RequireApiGroup("JobWriter")]
    public override async Task<Empty> DeleteJob(JobRequest request, ServerCallContext context)
    {
        var jobId = Guid.Parse(request.JobId);
        var rowsAffected = await _jobRepository.DeleteJobAsync(jobId, context.CancellationToken);

        if (rowsAffected == 0)
        {
            _logger.LogInformation("Job with ID {JobId} not found for deletion", request.JobId);
            throw new RpcException(new Status(StatusCode.NotFound, $"Job with ID {request.JobId} not found."));
        }

        _logger.LogInformation("Deleted job with ID {JobId}", jobId);
        return new Empty();
    }
}
