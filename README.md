# Urban Infrastructure Issue Reporting & Resolution System — Backend

Khởi tạo Backend (.NET 10, Clean Architecture / N-layer) + xác thực JWT cơ bản
cho Sprint 1 (Tuần 1 & 2).

## 1. Kiến trúc

```

### 2026-08-10 - Add SLA Policy CRUD

**Prompt:**

Implement CRUD for SLA policies based on Issue Type + Priority.

**Files changed:**

- `src/Domain/Entities/IssueType.cs`
- `src/Domain/Entities/IssuePriority.cs`
- `src/Domain/Entities/SlaPolicy.cs`
- `src/Application/DTOs/SlaPolicies/CreateSlaPolicyRequest.cs`
- `src/Application/DTOs/SlaPolicies/UpdateSlaPolicyRequest.cs`
- `src/Application/DTOs/SlaPolicies/SlaPolicyResponse.cs`
- `src/Application/Interfaces/ISlaPolicyService.cs`
- `src/Infrastructure/Services/SlaPolicyService.cs`
- `src/Infrastructure/Persistence/AppDbContext.cs` (DbSet + entity config)
- `src/Infrastructure/DependencyInjection.cs` (register service)
- `src/API/Controllers/SlaPoliciesController.cs`
- `src/Infrastructure/Migrations/20260810090000_AddSlaPolicies.cs`

**Changes:**

- Added SLA Policy entity and minimal IssueType / IssuePriority entities to support FK relations.
- Added DTOs and service interface for SLA CRUD.
- Implemented SlaPolicyService with business validation (existence, positive durations, firstResponse <= resolution, uniqueness).
- Added EF Core configuration with unique index on (IssueTypeId, PriorityId) and soft-delete filtering.
- Added migration to create IssueTypes, IssuePriorities and SlaPolicies tables.
- Added Admin-only API controller for CRUD operations.

**Status:**

Code changed and saved.

### 2026-08-12 - Add Escalation Rules CRUD

**Prompt:**

Implement CRUD for Escalation Rules mapping SLA Policy + overdue minutes to target department/role.

**Files changed:**

- `src/Domain/Entities/EscalationRule.cs`
- `src/Application/DTOs/EscalationRules/EscalationRuleDtos.cs`
- `src/Application/Interfaces/IEscalationRuleService.cs`
- `src/Infrastructure/Services/EscalationRuleService.cs`
- `src/Infrastructure/Persistence/AppDbContext.cs` (DbSet + entity config)
- `src/Infrastructure/DependencyInjection.cs` (register service)
- `src/API/Controllers/EscalationRulesController.cs`
- `src/Infrastructure/Migrations/20260812093000_AddEscalationRules.cs`

**Changes:**

- Added EscalationRule entity linking SLA policy, overdue minutes and target department/role.
- Added DTOs and service interface for EscalationRule CRUD.
- Implemented EscalationRuleService with validation (SLA existence, department existence, role existence via RoleManager), duplicate prevention, soft-delete handling.
- Added EF Core configuration with FK to SlaPolicies and Departments, unique constraint preventing identical rules.
- Added migration to create EscalationRules table.
- Added Admin-only API controller for CRUD operations.

**Status:**

Code changed and saved.

UrbanInfraSystem/
├── UrbanInfraSystem.sln
├── Dockerfile
├── docker-compose.yml
└── src/
    ├── Domain/              # Entities thuần, không phụ thuộc thư viện ngoài
    │   ├── Common/BaseEntity.cs
    │   ├── Entities/RefreshToken.cs
    │   └── Enums/Roles.cs
    ├── Application/         # Interfaces, DTOs — quy tắc nghiệp vụ, không biết Infra là gì
    │   ├── DTOs/Auth/
    │   └── Interfaces/ (IJwtService, IAuthService, ICurrentUserService)
    ├── Infrastructure/      # EF Core, ASP.NET Identity, JWT — implement các interface trên
    │   ├── Identity/ (ApplicationUser, ApplicationRole, JwtService, AuthService, CurrentUserService)
    │   ├── Persistence/AppDbContext.cs
    │   └── DependencyInjection.cs
    └── API/                 # Presentation layer — Controllers, Program.cs, appsettings
        ├── Controllers/ (AuthController, DemoController)
        └── Program.cs
```

Chiều phụ thuộc: `API → Infrastructure → Application → Domain`
(Domain không phụ thuộc gì; Application chỉ phụ thuộc Domain).

## 2. Đã có sẵn

- **Đăng ký/Đăng nhập** (`POST /api/auth/register`, `POST /api/auth/login`)
- **JWT access token** (ngắn hạn, 15 phút mặc định) + **refresh token** (dài hạn,
  7 ngày, lưu DB, hỗ trợ rotation khi refresh, thu hồi khi logout)
- **Phân quyền theo Role**: `Admin`, `DepartmentStaff`, `Citizen` (đúng 3 actor ở
  mục III đặc tả) — role được seed tự động khi ứng dụng khởi động
- Đăng ký công khai mặc định gán role `Citizen`; tài khoản `Admin`/`DepartmentStaff`
  sẽ được tạo qua API quản trị riêng ở Sprint 2 (không public)
- Swagger UI có nút **Authorize** để dán Bearer token test trực tiếp
- `DemoController` minh hoạ `[Authorize(Roles = "...")]` cho từng role
- Dockerfile + docker-compose (API + SQL Server) theo đề xuất công nghệ mục VI

## 3. Chạy thử (cần máy có cài .NET 10 SDK — môi trường tạo file này không có mạng
tới NuGet nên chưa `dotnet restore`/build thử được, bạn cần chạy các lệnh dưới trên máy mình)

```bash
# Restore & build
dotnet restore
dotnet build

# Tạo migration đầu tiên (cần cài dotnet-ef bản 10.x để khớp EF Core 10:
# dotnet tool install --global dotnet-ef --version 10.*
# hoặc nếu đã cài bản cũ: dotnet tool update --global dotnet-ef --version 10.*)
cd src/API
dotnet ef migrations add InitialCreate --project ../Infrastructure --startup-project .
dotnet ef database update --project ../Infrastructure --startup-project .

# Chạy API
dotnet run
# Mở https://localhost:5081/swagger
```

Hoặc chạy bằng Docker (đã kèm SQL Server, không cần cài SQL Server local):

```bash
docker compose up --build
```

## 4. Test nhanh luồng JWT

1. `POST /api/auth/register` → nhận `accessToken` + `refreshToken`
2. Bấm **Authorize** trên Swagger, nhập `Bearer <accessToken>`
3. Gọi `GET /api/auth/me` hoặc `GET /api/demo/citizen-only` → 200 OK
4. Khi access token hết hạn: `POST /api/auth/refresh-token` với `accessToken` cũ +
   `refreshToken` → nhận cặp token mới
5. `POST /api/auth/logout` với `refreshToken` hiện tại → refresh token bị thu hồi

## 5. Lưu ý bảo mật trước khi deploy

- **Đổi `Jwt:Secret`** trong `appsettings.json` — không commit secret thật lên Git.
  Dùng `dotnet user-secrets` khi dev, biến môi trường / Key Vault khi deploy.
- Đổi connection string SQL Server phù hợp môi trường thật (mục VI đề xuất
  Render/Azure Free Tier/Railway).
- Cập nhật `Cors:AllowedOrigins` đúng origin của Frontend khi deploy.

## 6. Việc tiếp theo (Sprint 1, còn lại)

- Thiết kế ERD đầy đủ cho các entity nghiệp vụ (Issue, Category, Department, Location...)
- Viết API contract (OpenAPI) cho các endpoint CRUD chính ở mục IV
- Mockup UI/UX cho các màn hình chính
- Thiết lập CI/CD cơ bản (GitHub Actions: build + test)
