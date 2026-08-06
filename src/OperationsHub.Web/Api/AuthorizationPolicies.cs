using OperationsHub.Infrastructure.Identity;

namespace OperationsHub.Web.Api;

public static class AuthorizationPolicies
{
    public const string Administrator = RoleNames.Administrator;
    public const string AdministratorImpersonation = "DevelopmentAdministratorImpersonation";
    public const string ManagerOrAdministrator = "ManagerOrAdministrator";
    public const string OperationsUser = "OperationsUser";
}
