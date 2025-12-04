using Grpc.Data.Entities;

namespace Grpc.Data.Contracts;

public interface IJobRepository
{
    Task CreateJobAsync(JobDto jobDto, CancellationToken token);
    Task<JobDto?> GetJobByIdAsync(Guid jobId, CancellationToken token);
    Task<IAsyncEnumerable<JobDto>> GetJobsAsync(int limit, CancellationToken token);
    Task<int> DeleteJobAsync(Guid jobId, CancellationToken token);
}
