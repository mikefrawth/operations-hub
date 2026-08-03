namespace OperationsHub.Application.Impersonation;

public sealed record ImpersonationTargetDto(string UserId, string DisplayName, string Email, string Role);

public sealed record ImpersonationRequestContext(string? RemoteIpAddress, string TraceIdentifier);

public enum ImpersonationOperationStatus
{
    Success,
    Forbidden,
    InvalidTarget,
}

public sealed record ImpersonationOperationResult(
    ImpersonationOperationStatus Status,
    ImpersonationTargetDto? Target)
{
    public bool Succeeded => Status == ImpersonationOperationStatus.Success;
}
