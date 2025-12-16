using FluentValidation;
using Grpc.Service.Services;

namespace Grpc.Service.Validators;

internal sealed class JobRequestValidator : AbstractValidator<JobRequest>
{
    public JobRequestValidator()
    {
        RuleFor(request => request.JobId)
            .NotEqual(Guid.Empty.ToString()).WithMessage("JobId cannot be an empty GUID.");
    }
}

internal sealed class JobCreateRequestValidator : AbstractValidator<JobCreateRequest>
{
    public JobCreateRequestValidator()
    {
        RuleFor(request => request.JobName)
            .NotEmpty().WithMessage("JobName is required.")
            .MaximumLength(100).WithMessage("JobName cannot exceed 100 characters.");
        RuleFor(request => request.JobDescription)
            .NotEmpty().WithMessage("JobDescription is required.")
            .MaximumLength(200).WithMessage("JobDescription cannot exceed 200 characters.");
    }
}

internal sealed class JobListOptionsValidator : AbstractValidator<JobListOptions>
{
    public JobListOptionsValidator()
    {
        RuleFor(options => options.Limit)
            .GreaterThan(0).WithMessage("Limit must be greater than zero.")
            .LessThanOrEqualTo(100).WithMessage("Limit cannot exceed 100.");
    }
}
