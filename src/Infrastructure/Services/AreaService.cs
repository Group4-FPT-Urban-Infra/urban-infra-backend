using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;
using UrbanInfraSystem.Application.DTOs.Areas;
using UrbanInfraSystem.Application.Interfaces;
using UrbanInfraSystem.Domain.Entities;
using UrbanInfraSystem.Infrastructure.Persistence;

namespace UrbanInfraSystem.Infrastructure.Services;

public class AreaService : IAreaService
{
    private readonly AppDbContext _context;
    private readonly GeoJsonReader _geoJsonReader;
    private readonly GeoJsonWriter _geoJsonWriter;

    public AreaService(AppDbContext context)
    {
        _context = context;
        _geoJsonReader = new GeoJsonReader();
        _geoJsonWriter = new GeoJsonWriter();
    }

    public async Task<List<AreaResponse>> GetAreasAsync(AreaFilterRequest filter, CancellationToken cancellationToken = default)
    {
        var query = _context.Areas
            .Include(a => a.ParentArea)
            .AsNoTracking();

        if (filter.ParentAreaId.HasValue)
        {
            query = query.Where(a => a.ParentAreaId == filter.ParentAreaId.Value);
        }

        if (!string.IsNullOrWhiteSpace(filter.AreaType))
        {
            query = query.Where(a => a.AreaType.ToLower() == filter.AreaType.ToLower());
        }

        if (filter.IsActive.HasValue)
        {
            query = query.Where(a => a.IsActive == filter.IsActive.Value);
        }

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var search = filter.Search.Trim().ToLower();
            query = query.Where(a => a.AreaName.ToLower().Contains(search) || a.AreaCode.ToLower().Contains(search));
        }

        var areas = await query
            .OrderBy(a => a.AreaCode)
            .ToListAsync(cancellationToken);

        return areas.Select(MapToResponse).ToList();
    }

    public async Task<AreaResponse?> GetAreaByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var area = await _context.Areas
            .Include(a => a.ParentArea)
            .Include(a => a.SubAreas)
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.AreaId == id, cancellationToken);

        if (area == null) return null;

        var response = MapToResponse(area);
        if (area.SubAreas.Any())
        {
            response.SubAreas = area.SubAreas.Select(MapToResponse).ToList();
        }

        return response;
    }

    public async Task<AreaResponse> CreateAreaAsync(CreateAreaRequest request, CancellationToken cancellationToken = default)
    {
        if (request.ParentAreaId.HasValue)
        {
            var parentExists = await _context.Areas.AnyAsync(a => a.AreaId == request.ParentAreaId.Value, cancellationToken);
            if (!parentExists)
            {
                throw new ArgumentException($"Khu vực cha (ID = {request.ParentAreaId}) không tồn tại.");
            }
        }

        var codeExists = await _context.Areas.AnyAsync(a => a.AreaCode == request.AreaCode, cancellationToken);
        if (codeExists)
        {
            throw new InvalidOperationException($"Mã khu vực '{request.AreaCode}' đã tồn tại.");
        }

        var boundaryGeometry = ParseGeoJsonBoundary(request.Boundary);
        var (lat, lon) = CalculateCentroid(boundaryGeometry, request.CentroidLatitude, request.CentroidLongitude);

        var area = new Area
        {
            ParentAreaId = request.ParentAreaId,
            AreaCode = request.AreaCode.Trim(),
            AreaName = request.AreaName.Trim(),
            AreaType = request.AreaType.Trim(),
            Boundary = boundaryGeometry,
            CentroidLatitude = lat,
            CentroidLongitude = lon,
            IsActive = request.IsActive,
            CreatedAt = DateTime.UtcNow
        };

        _context.Areas.Add(area);
        await _context.SaveChangesAsync(cancellationToken);

        return await GetAreaByIdAsync(area.AreaId, cancellationToken)
            ?? MapToResponse(area);
    }

    public async Task<AreaResponse> UpdateAreaAsync(int id, UpdateAreaRequest request, CancellationToken cancellationToken = default)
    {
        var area = await _context.Areas.FirstOrDefaultAsync(a => a.AreaId == id, cancellationToken);
        if (area == null)
        {
            throw new KeyNotFoundException($"Không tìm thấy khu vực có ID = {id}");
        }

        if (request.ParentAreaId.HasValue)
        {
            if (request.ParentAreaId.Value == id)
            {
                throw new ArgumentException("Khu vực không thể tự nhận chính mình làm khu vực cha.");
            }

            var parentExists = await _context.Areas.AnyAsync(a => a.AreaId == request.ParentAreaId.Value, cancellationToken);
            if (!parentExists)
            {
                throw new ArgumentException($"Khu vực cha (ID = {request.ParentAreaId}) không tồn tại.");
            }
        }

        var codeExists = await _context.Areas.AnyAsync(a => a.AreaCode == request.AreaCode && a.AreaId != id, cancellationToken);
        if (codeExists)
        {
            throw new InvalidOperationException($"Mã khu vực '{request.AreaCode}' đã tồn tại ở khu vực khác.");
        }

        var boundaryGeometry = ParseGeoJsonBoundary(request.Boundary);
        var (lat, lon) = CalculateCentroid(boundaryGeometry, request.CentroidLatitude, request.CentroidLongitude);

        area.ParentAreaId = request.ParentAreaId;
        area.AreaCode = request.AreaCode.Trim();
        area.AreaName = request.AreaName.Trim();
        area.AreaType = request.AreaType.Trim();
        area.Boundary = boundaryGeometry;
        area.CentroidLatitude = lat;
        area.CentroidLongitude = lon;
        area.IsActive = request.IsActive;
        area.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        return await GetAreaByIdAsync(area.AreaId, cancellationToken)
            ?? MapToResponse(area);
    }

    public async Task<bool> DeleteAreaAsync(int id, CancellationToken cancellationToken = default)
    {
        var area = await _context.Areas
            .Include(a => a.SubAreas)
            .FirstOrDefaultAsync(a => a.AreaId == id, cancellationToken);

        if (area == null)
        {
            return false;
        }

        if (area.SubAreas.Any())
        {
            throw new InvalidOperationException($"Không thể xóa khu vực '{area.AreaName}' vì đang có {area.SubAreas.Count} khu vực con phụ thuộc.");
        }

        _context.Areas.Remove(area);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    private Geometry? ParseGeoJsonBoundary(JsonNode? boundaryNode)
    {
        if (boundaryNode == null) return null;

        try
        {
            string jsonString = boundaryNode.ToJsonString();
            if (string.IsNullOrWhiteSpace(jsonString) || jsonString == "null") return null;

            var geometry = _geoJsonReader.Read<Geometry>(jsonString);
            if (geometry != null)
            {
                geometry.SRID = 4326; // Default WGS84 GPS coordinate system
            }
            return geometry;
        }
        catch (Exception ex)
        {
            throw new ArgumentException($"Dữ liệu GeoJSON boundary không hợp lệ: {ex.Message}");
        }
    }

    private (decimal? lat, decimal? lon) CalculateCentroid(Geometry? geometry, decimal? inputLat, decimal? inputLon)
    {
        if (inputLat.HasValue && inputLon.HasValue)
        {
            return (inputLat, inputLon);
        }

        if (geometry != null && !geometry.IsEmpty)
        {
            var centroid = geometry.Centroid;
            return (
                inputLat ?? (decimal)Math.Round(centroid.Y, 6),
                inputLon ?? (decimal)Math.Round(centroid.X, 6)
            );
        }

        return (inputLat, inputLon);
    }

    private AreaResponse MapToResponse(Area area)
    {
        JsonNode? boundaryJsonNode = null;
        if (area.Boundary != null)
        {
            try
            {
                string json = _geoJsonWriter.Write(area.Boundary);
                boundaryJsonNode = JsonNode.Parse(json);
            }
            catch
            {
                // Fallback if formatting fails
            }
        }

        return new AreaResponse
        {
            AreaId = area.AreaId,
            ParentAreaId = area.ParentAreaId,
            ParentAreaName = area.ParentArea?.AreaName,
            AreaCode = area.AreaCode,
            AreaName = area.AreaName,
            AreaType = area.AreaType,
            Boundary = boundaryJsonNode,
            CentroidLatitude = area.CentroidLatitude,
            CentroidLongitude = area.CentroidLongitude,
            IsActive = area.IsActive,
            CreatedAt = area.CreatedAt,
            UpdatedAt = area.UpdatedAt
        };
    }
}
