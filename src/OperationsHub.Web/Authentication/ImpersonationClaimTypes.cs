namespace OperationsHub.Web.Authentication;

public static class ImpersonationClaimTypes
{
    private const string Prefix = "urn:operationshub:impersonation:";

    public const string OriginalAdministratorId = Prefix + "original-administrator-id";
    public const string OriginalAdministratorDisplayName = Prefix + "original-administrator-display-name";
    public const string TargetDisplayName = Prefix + "target-display-name";
    public const string StartedAtUtc = Prefix + "started-at-utc";
}
