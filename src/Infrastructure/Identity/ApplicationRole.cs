using Microsoft.AspNetCore.Identity;

namespace UrbanInfraSystem.Infrastructure.Identity;

public class ApplicationRole : IdentityRole
{
    public string? Description { get; set; }
}
