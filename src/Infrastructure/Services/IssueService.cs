using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using UrbanInfraSystem.Application.DTOs.Issues;
using UrbanInfraSystem.Application.Interfaces;
using UrbanInfraSystem.Infrastructure.Persistence;

namespace UrbanInfraSystem.Infrastructure.Services;

public class IssueService : IIssueService
{
    private readonly AppDbContext _db;

    // Max results to return when no pagination configured
    private const int DefaultMaxResults = 10;

    public IssueService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<ApiResponse<IReadOnlyList<NearbyIssueResponse>>> FindNearbyAsync(FindNearbyIssuesRequest request, CancellationToken ct = default)
    {
        // Basic validation
        if (request.IssueTypeId <= 0) return new ApiResponse<IReadOnlyList<NearbyIssueResponse>> { Success = false, Message = "Invalid IssueTypeId" };
        if (request.Latitude < -90m || request.Latitude > 90m) return new ApiResponse<IReadOnlyList<NearbyIssueResponse>> { Success = false, Message = "Latitude out of range" };
        if (request.Longitude < -180m || request.Longitude > 180m) return new ApiResponse<IReadOnlyList<NearbyIssueResponse>> { Success = false, Message = "Longitude out of range" };
        if (request.RadiusMeters <= 0) return new ApiResponse<IReadOnlyList<NearbyIssueResponse>> { Success = false, Message = "RadiusMeters must be > 0" };

        // Check IssueType existence and active flag
        var issueTypeExists = await _db.IssueTypes.AnyAsync(it => it.IssueTypeId == request.IssueTypeId && it.IsActive, ct);
        if (!issueTypeExists) return new ApiResponse<IReadOnlyList<NearbyIssueResponse>> { Success = false, Message = "IssueType not found" };

        // Bounding box optimization
        var lat = (double)request.Latitude;
        var lon = (double)request.Longitude;
        var radius = request.RadiusMeters; // meters

        // Earth radius in meters
        const double earth = 6371000.0;

        // angular distance in radians on earth's surface
        var angDist = radius / earth;

        var latRad = DegreeToRadian(lat);

        var minLat = lat - RadianToDegree(angDist);
        var maxLat = lat + RadianToDegree(angDist);

        // Lon delta depends on latitude
        var deltaLon = Math.Asin(Math.Sin(angDist) / Math.Cos(latRad));
        var minLon = lon - RadianToDegree(deltaLon);
        var maxLon = lon + RadianToDegree(deltaLon);

        // Query candidates from DB
        var candidatesQuery = _db.Issues
            .AsNoTracking()
            .Where(i => i.IssueTypeId == request.IssueTypeId && !i.IsArchived)
            .Where(i => i.Latitude >= (decimal)minLat && i.Latitude <= (decimal)maxLat)
            .Where(i => i.Longitude >= (decimal)minLon && i.Longitude <= (decimal)maxLon)
            ;

        // Limit number of candidates to avoid loading huge datasets; fetch top 200 by ReportedAt desc as heuristic
        var candidates = await candidatesQuery
            .OrderByDescending(i => i.ReportedAt)
            .Take(200)
            .ToListAsync(ct);

        // Compute exact Haversine distances in memory, filter and sort
        var nearby = candidates
            .Select(i => new NearbyIssueResponse
            {
                Id = i.IssueId,
                PublicCode = i.PublicCode,
                Title = i.Title,
                IssueType = new LookupItemResponse { Id = i.IssueTypeId, Name = i.IssueType?.TypeName ?? string.Empty, Code = i.IssueType?.TypeCode },
                Area = new LookupItemResponse { Id = i.AreaId ?? 0, Name = string.Empty },
                Priority = new LookupItemResponse { Id = i.PriorityId ?? 0, Name = i.Priority?.PriorityName ?? string.Empty },
                Status = new LookupItemResponse { Id = i.StatusId ?? 0, Name = i.Status?.StatusName ?? string.Empty },
                Latitude = i.Latitude,
                Longitude = i.Longitude,
                ThumbnailUrl = i.ThumbnailUrl,
                UpvoteCount = i.UpvoteCount,
                HasUpvoted = false,
                ReportedAt = i.ReportedAt,
                DistanceMeters = HaversineDistanceMeters(lat, lon, (double)i.Latitude, (double)i.Longitude)
            })
            .Where(x => x.DistanceMeters <= radius)
            .OrderBy(x => x.DistanceMeters)
            .Take(request.Limit > 0 ? request.Limit : DefaultMaxResults)
            .ToList();

        return new ApiResponse<IReadOnlyList<NearbyIssueResponse>> { Success = true, Data = nearby };
    }

    private static double DegreeToRadian(double deg) => deg * Math.PI / 180.0;
    private static double RadianToDegree(double rad) => rad * 180.0 / Math.PI;

    private static double HaversineDistanceMeters(double lat1, double lon1, double lat2, double lon2)
    {
        const double R = 6371000.0; // Earth radius meters
        var dLat = DegreeToRadian(lat2 - lat1);
        var dLon = DegreeToRadian(lon2 - lon1);
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(DegreeToRadian(lat1)) * Math.Cos(DegreeToRadian(lat2)) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return R * c;
    }
}
