using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using UrbanInfraSystem.Application.Interfaces;
using UrbanInfraSystem.Infrastructure.Identity;
using UrbanInfraSystem.Infrastructure.Persistence;
using UrbanInfraSystem.Infrastructure.Services;
using UrbanInfraSystem.Infrastructure.Services.Elaboration;

namespace UrbanInfraSystem.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // --- Persistence ---
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection"),
                x => x.UseNetTopologySuite().MigrationsAssembly("UrbanInfraSystem.Infrastructure")));

        // --- Identity ---
        services.AddIdentity<ApplicationUser, ApplicationRole>(options =>
        {
            options.Password.RequiredLength = 6;
            options.Password.RequireNonAlphanumeric = false;
            options.Password.RequireUppercase = false;
            options.User.RequireUniqueEmail = true;
            options.SignIn.RequireConfirmedEmail = false; // có thể bật khi làm email verification
        })
            .AddEntityFrameworkStores<AppDbContext>()
            .AddDefaultTokenProviders();

        // --- JWT ---
        var jwtSection = configuration.GetSection(JwtSettings.SectionName);
        services.Configure<JwtSettings>(jwtSection);
        var jwtSettings = jwtSection.Get<JwtSettings>()!;

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
            .AddJwtBearer(options =>
            {
                options.RequireHttpsMetadata = false;
                options.SaveToken = true;
                // Tắt việc tự động đổi tên claim từ short ("role", "sub", "email")
                // sang URI dài (ClaimTypes.Role, ClaimTypes.NameIdentifier, ...).
                // Nhờ đó RoleClaimType = "role" hoạt động đúng với JWT được tạo ra.
                options.MapInboundClaims = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtSettings.Issuer,
                    ValidAudience = jwtSettings.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Secret)),
                    RoleClaimType = "role",  // Map "role" claim từ JWT thành ClaimTypes.Role
                    ClockSkew = TimeSpan.FromMinutes(1)
                };
            });

        services.AddAuthorization();
        services.AddSignalR();

        // --- Application services ---
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<IJwtService, JwtService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IAreaService, AreaService>();
        services.AddScoped<IIssueTypeService, IssueTypeService>();
        services.AddScoped<IIssuePriorityService, IssuePriorityService>();
        services.AddScoped<IIssueStatusService, IssueStatusService>();
        services.AddScoped<IDepartmentService, DepartmentService>();
        services.AddScoped<IDepartmentMemberService, DepartmentMemberService>();
        services.AddScoped<ISlaPolicyService, SlaPolicyService>();

        services.AddScoped<IUserManagementService, UserManagementService>();
        services.AddScoped<IIssueService, IssueService>();
        services.AddScoped<IReportService, ReportService>();
        services.AddScoped<IIssueUpvoteService, IssueUpvoteService>();
        services.AddScoped<IRoutingRuleService, RoutingRuleService>();
        services.AddScoped<IIssueAssignmentService, IssueAssignmentService>();

        services.AddScoped<IStaffService, StaffService>();

        services.AddScoped<IIssueAssignmentMemberService, IssueAssignmentMemberService>();
        services.AddScoped<INotificationService, NotificationService>();

        // --- Department Manager services ---
        services.AddScoped<IDepartmentManagerDashboardService, DepartmentManagerDashboardService>();
        services.AddScoped<IDepartmentManagerIssueService, DepartmentManagerIssueService>();
        services.AddScoped<IDepartmentManagerSlaService, DepartmentManagerSlaService>();
        services.AddScoped<IDepartmentManagerStaffService, DepartmentManagerStaffService>();


        // --- Background Hosted Services ---
        services.AddHostedService<UrbanInfraSystem.Infrastructure.BackgroundServices.SlaCheckBackgroundService>();

        services.AddScoped<IIssueService, IssueService>();
        services.AddScoped<IEscalationRuleService, EscalationRuleService>();
        services.AddScoped<IEscalationEventService, EscalationEventService>();
        // Escalation processor and background worker
        services.AddScoped<EscalationProcessor>();
        services.AddHostedService<EscalationBackgroundService>();


        return services;
    }
}
