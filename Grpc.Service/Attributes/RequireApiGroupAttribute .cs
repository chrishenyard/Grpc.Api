using Microsoft.AspNetCore.Authorization;

namespace Grpc.Service.Attributes;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public sealed class RequireApiGroupAttribute(string groupName) : AuthorizeAttribute
{
    public string GroupName { get; } = groupName;
}

