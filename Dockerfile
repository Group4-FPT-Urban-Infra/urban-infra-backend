# ---- Build stage ----
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY UrbanInfraSystem.sln .
COPY src/Domain/UrbanInfraSystem.Domain.csproj src/Domain/
COPY src/Application/UrbanInfraSystem.Application.csproj src/Application/
COPY src/Infrastructure/UrbanInfraSystem.Infrastructure.csproj src/Infrastructure/
COPY src/API/UrbanInfraSystem.API.csproj src/API/

RUN dotnet restore UrbanInfraSystem.sln

COPY . .
RUN dotnet publish src/API/UrbanInfraSystem.API.csproj -c Release -o /app/publish --no-restore

# ---- Runtime stage ----
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "UrbanInfraSystem.API.dll"]
