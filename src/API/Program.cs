using Microsoft.OpenApi.Models;
using System.Reflection;
using UrbanInfraSystem.Domain.Enums;
using UrbanInfraSystem.Infrastructure;
using UrbanInfraSystem.Infrastructure.Identity;

var builder = WebApplication.CreateBuilder(args);

// ---- Services ----
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        // Trong .env / appsettings, cấu hình đúng origin của Angular/React/Vue dev server.
        var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
                              ?? new[] { "http://localhost:4200" };

        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Urban Infrastructure Issue Reporting API",
        Version = "v1",
        Description = "API cho hệ thống Báo cáo & Xử lý Sự cố Hạ tầng Đô thị"
    });

    // Nút "Authorize" trên Swagger UI để test API có JWT.
    var jwtScheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Description = "Nhập: Bearer {access_token}",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
    };
    options.AddSecurityDefinition("Bearer", jwtScheme);
    options.AddSecurityRequirement(new OpenApiSecurityRequirement { { jwtScheme, Array.Empty<string>() } });

    var xmlFileName = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFileName));
});

var app = builder.Build();

// ---- Seed roles + Admin account khi khởi động ----
using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider.GetRequiredService<Microsoft.AspNetCore.Identity.RoleManager<ApplicationRole>>();
    var userManager = scope.ServiceProvider.GetRequiredService<Microsoft.AspNetCore.Identity.UserManager<ApplicationUser>>();
    var config      = scope.ServiceProvider.GetRequiredService<IConfiguration>();

    // 1. Đảm bảo tất cả roles tồn tại
    foreach (var roleName in Roles.All)
    {
        if (!await roleManager.RoleExistsAsync(roleName))
            await roleManager.CreateAsync(new ApplicationRole { Name = roleName });
    }

    // 2. Seed / Fix tài khoản Admin đầu tiên
    //    Đọc thông tin từ section "SeedAdmin" trong appsettings.json.
    //    Override bằng biến môi trường: SeedAdmin__Email, SeedAdmin__Password, ...
    var seedEmail    = config["SeedAdmin:Email"];
    var seedPassword = config["SeedAdmin:Password"];
    var seedFullName = config["SeedAdmin:FullName"] ?? "Quản trị viên";

    if (!string.IsNullOrWhiteSpace(seedEmail) && !string.IsNullOrWhiteSpace(seedPassword))
    {
        var existingAdmin = await userManager.FindByEmailAsync(seedEmail);

        if (existingAdmin is null)
        {
            // Tạo mới tài khoản Admin
            var adminUser = new ApplicationUser
            {
                UserName     = seedEmail,
                Email        = seedEmail,
                FullName     = seedFullName,
                IsActive     = true,
                CreatedAtUtc = DateTime.UtcNow
            };

            var createResult = await userManager.CreateAsync(adminUser, seedPassword);
            if (createResult.Succeeded)
            {
                await userManager.AddToRoleAsync(adminUser, Roles.Admin);
                Console.WriteLine($"[Seed] ✔ Tài khoản Admin đã được tạo: {seedEmail}");
            }
            else
            {
                var errs = string.Join(", ", createResult.Errors.Select(e => e.Description));
                Console.WriteLine($"[Seed] ✘ Không thể tạo Admin: {errs}");
            }
        }
        else
        {
            // User đã tồn tại — đảm bảo đang có role Admin
            // (xử lý trường hợp user được tạo trước khi seed hoặc bị mất role)
            var hasAdminRole = await userManager.IsInRoleAsync(existingAdmin, Roles.Admin);
            if (!hasAdminRole)
            {
                await userManager.AddToRoleAsync(existingAdmin, Roles.Admin);
                Console.WriteLine($"[Seed] ✔ Đã gán role Admin cho tài khoản: {seedEmail}");
            }
            else
            {
                Console.WriteLine($"[Seed] ℹ Admin đã tồn tại và có đầy đủ quyền: {seedEmail}");
            }
        }
    }
}

// ---- Middleware pipeline ----
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors("AllowFrontend");
app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<UrbanInfraSystem.Infrastructure.Hubs.NotificationHub>("/hubs/notifications");

app.Run();
