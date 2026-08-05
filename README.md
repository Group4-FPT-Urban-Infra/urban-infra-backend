# Urban Infrastructure Issue Reporting & Resolution System — Backend

Khởi tạo Backend (.NET 10, Clean Architecture / N-layer) + xác thực JWT cơ bản
cho Sprint 1 (Tuần 1 & 2).

## 1. Kiến trúc

```
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
