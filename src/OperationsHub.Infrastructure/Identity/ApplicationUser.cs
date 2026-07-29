using Microsoft.AspNetCore.Identity;

namespace OperationsHub.Infrastructure.Identity;

public sealed class ApplicationUser : IdentityUser
{
    public string DisplayName { get; set; } = null!;
}
