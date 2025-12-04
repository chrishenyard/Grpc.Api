using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Grpc.Data.Entities;

[Table("Job")]
public class Job
{
    [Key]
    public Guid JobId { get; set; }

    [Column(TypeName = "nvarchar(100)")]
    public required string Name { get; set; }

    [Column(TypeName = "nvarchar(200)")]
    public required string Description { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
}

public sealed record JobDto
{
    public Guid JobId { get; init; }
    public string Name { get; init; } = string.Empty;
    public bool IsActive { get; init; } = true;
    public string Description { get; init; } = string.Empty;
    public DateTime CreatedUtc { get; init; }

    private JobDto() { }

    private JobDto(Guid jobId, string name, bool isActive, string description, DateTime createdUtc)
    {
        ValidateOrThrow(jobId, name, description, createdUtc);

        JobId = jobId;
        Name = name;
        IsActive = isActive;
        Description = description;
        CreatedUtc = createdUtc;
    }

    private JobDto(string name, bool isActive, string description, DateTime createdUtc)
    {
        ValidateOrThrow(name, description, createdUtc);

        Name = name;
        IsActive = isActive;
        Description = description;
        CreatedUtc = createdUtc;
    }

    public static JobDto Create(string name, string description)
        => new(name, true, description, DateTime.UtcNow);

    public static implicit operator JobDto(Job entity)
        => new(entity.JobId, entity.Name, entity.IsActive, entity.Description, entity.CreatedUtc);

    public static implicit operator Job(JobDto dto)
        => new()
        {
            JobId = dto.JobId,
            Name = dto.Name,
            IsActive = dto.IsActive,
            Description = dto.Description,
            CreatedUtc = dto.CreatedUtc
        };

    private static void ValidateOrThrow(Guid jobId, string name, string description, DateTime createdUtc)
    {
        if (jobId == Guid.Empty)
            throw new ArgumentException("JobId must be provided.", nameof(jobId));

        ValidateOrThrow(name, description, createdUtc);
    }

    private static void ValidateOrThrow(string name, string description, DateTime createdUtc)
    {
        const int maxNameLength = 100;
        if (name.Length > maxNameLength)
            throw new ArgumentException($"Name cannot be longer than {maxNameLength} characters.", nameof(name));

        const int maxDescriptionLength = 200;
        if (description.Length > maxDescriptionLength)
            throw new ArgumentException($"Description cannot be longer than {maxDescriptionLength} characters.", nameof(description));

        if (createdUtc == default)
            throw new ArgumentException("CreatedUtc must be set.", nameof(createdUtc));
    }
}

public static class JobExtensions
{
    public static JobDto ToJobDto(this Job entity) => entity;
    public static Job ToJob(this JobDto dto) => dto;
}
