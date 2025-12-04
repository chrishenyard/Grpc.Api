using Grpc.Data.Contracts;
using Grpc.Data.DbContexts;
using Grpc.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Grpc.Data.Repositories;

public class JobRepository(GrpcDbContext dbContext) : IJobRepository
{
    private readonly GrpcDbContext _dbContext = dbContext;

    public async Task CreateJobAsync(JobDto jobDto, CancellationToken token)
    {
        var job = jobDto.ToJob();
        await _dbContext.Jobs.AddAsync(job, token);
        await _dbContext.SaveChangesAsync(token);
    }

    public async Task<IAsyncEnumerable<JobDto>> GetJobsAsync(int limit, CancellationToken token)
    {
        return _dbContext.Jobs
        .AsNoTracking()
        .Select(j => j.ToJobDto())
        .Take(limit)
        .AsAsyncEnumerable();
    }

    public async Task<JobDto?> GetJobByIdAsync(Guid jobId, CancellationToken token)
    {
        var jobDto = await _dbContext.Jobs
            .FirstOrDefaultAsync(j => j.JobId == jobId, token);
        return jobDto?.ToJobDto();
    }

    public async Task<int> DeleteJobAsync(Guid jobId, CancellationToken token)
    {
        var rowsAffected = await _dbContext.Jobs
            .Where(j => j.JobId == jobId)
            .IgnoreQueryFilters()
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(j => j.IsActive, false),
            cancellationToken: token);

        return rowsAffected;
    }
}
