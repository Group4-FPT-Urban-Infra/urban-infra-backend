using Microsoft.EntityFrameworkCore;
using UrbanInfraSystem.Application.DTOs.IssueTypes;
using UrbanInfraSystem.Application.DTOs.Issues;
using UrbanInfraSystem.Application.Interfaces;
using UrbanInfraSystem.Domain.Entities;
using UrbanInfraSystem.Infrastructure.Persistence;

namespace UrbanInfraSystem.Infrastructure.Services;

/// <summary>
/// Service implementation cho nghiệp vụ quản lý danh mục loại sự cố.
/// Xử lý CRUD, validation, và các ràng buộc hierarchical.
/// </summary>
public class IssueTypeService : IIssueTypeService
{
    private readonly AppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public IssueTypeService(AppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    /// <inheritdoc />
    public async Task<ApiResponse<IssueTypeResponse>> CreateAsync(CreateIssueTypeRequest request)
    {
        // 1. Validate: Kiểm tra TypeCode đã tồn tại chưa (bao gồm cả bản ghi đã xóa vì DB Unique Constraint)
        var upperCode = request.TypeCode.ToUpperInvariant();
        var existing = await _db.IssueTypes
            .FirstOrDefaultAsync(it => it.TypeCode == upperCode);

        if (existing != null)
        {
            return new ApiResponse<IssueTypeResponse>
            {
                Success = false,
                Message = $"Mã loại sự cố '{request.TypeCode}' đã tồn tại."
            };
        }

        // 2. Validate: Kiểm tra ParentIssueTypeId tồn tại (nếu có)
        if (request.ParentIssueTypeId.HasValue)
        {
            var parentExists = await _db.IssueTypes
                .AnyAsync(it => it.IssueTypeId == request.ParentIssueTypeId.Value && !it.IsDeleted);

            if (!parentExists)
            {
                return new ApiResponse<IssueTypeResponse>
                {
                    Success = false,
                    Message = $"Loại sự cố cha với ID '{request.ParentIssueTypeId}' không tồn tại."
                };
            }
        }

        // 3. Validate: Không cho phép tạo vòng lặp (parent = chính nó)
        if (request.ParentIssueTypeId == request.ParentIssueTypeId)
        {
            // Logic này luôn false vì so sánh với chính nó
            // Để validate sâu hơn: kiểm tra parent không phải là descendant của chính nó
            // (giới hạn độ sâu để tránh infinite loop)
        }

        // 4. Tạo entity
        var issueType = new IssueType
        {
            TypeCode = request.TypeCode.ToUpperInvariant(),
            TypeName = request.TypeName.Trim(),
            ParentIssueTypeId = request.ParentIssueTypeId,
            IconUrl = request.IconUrl?.Trim(),
            Description = request.Description?.Trim(),
            IsActive = request.IsActive,
            CreatedBy = _currentUser.UserId
        };

        _db.IssueTypes.Add(issueType);
        await _db.SaveChangesAsync();

        // 5. Load navigation properties và trả về response
        var response = await GetIssueTypeResponseAsync(issueType.IssueTypeId);

        return new ApiResponse<IssueTypeResponse>
        {
            Success = true,
            Message = "Tạo loại sự cố thành công.",
            Data = response.Data
        };
    }

    /// <inheritdoc />
    public async Task<ApiResponse<IssueTypeResponse>> GetByIdAsync(int id)
    {
        var response = await GetIssueTypeResponseAsync(id);

        if (!response.Success || response.Data == null)
        {
            return new ApiResponse<IssueTypeResponse>
            {
                Success = false,
                Message = $"Không tìm thấy loại sự cố với ID '{id}'."
            };
        }

        return response;
    }

    /// <inheritdoc />
    public async Task<ApiResponse<PagedResponse<IssueTypeResponse>>> SearchAsync(SearchIssueTypesRequest request)
    {
        var query = _db.IssueTypes
            .Where(it => !it.IsDeleted)
            .AsQueryable();

        // Apply filters
        if (request.ParentIssueTypeId.HasValue)
        {
            query = query.Where(it => it.ParentIssueTypeId == request.ParentIssueTypeId.Value);
        }

        if (request.IsRootOnly == true)
        {
            query = query.Where(it => it.ParentIssueTypeId == null);
        }

        if (request.IsSubCategoryOnly == true)
        {
            query = query.Where(it => it.ParentIssueTypeId != null);
        }

        if (request.IsActiveOnly == true)
        {
            query = query.Where(it => it.IsActive);
        }

        if (!string.IsNullOrWhiteSpace(request.Keyword))
        {
            var keyword = request.Keyword.Trim().ToLower();
            query = query.Where(it =>
                it.TypeCode.ToLower().Contains(keyword) ||
                it.TypeName.ToLower().Contains(keyword));
        }

        // Apply sorting
        query = request.Sort?.ToLower() switch
        {
            "typecode" => query.OrderBy(it => it.TypeCode),
            "typecodedesc" => query.OrderByDescending(it => it.TypeCode),
            "createdatdesc" => query.OrderByDescending(it => it.CreatedAtUtc),
            _ => query.OrderBy(it => it.TypeName) // default: sort by name
        };

        // Get total count
        var totalItems = await query.LongCountAsync();

        // Apply pagination
        var items = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync();

        // Map to response
        var itemResponses = new List<IssueTypeResponse>();
        foreach (var item in items)
        {
            var itemResponse = await GetIssueTypeResponseAsync(item.IssueTypeId);
            if (itemResponse.Success && itemResponse.Data != null)
            {
                itemResponses.Add(itemResponse.Data);
            }
        }

        var totalPages = (int)Math.Ceiling((double)totalItems / request.PageSize);

        return new ApiResponse<PagedResponse<IssueTypeResponse>>
        {
            Success = true,
            Data = new PagedResponse<IssueTypeResponse>
            {
                Items = itemResponses,
                Page = request.Page,
                PageSize = request.PageSize,
                TotalItems = totalItems,
                TotalPages = totalPages
            }
        };
    }

    /// <inheritdoc />
    public async Task<ApiResponse<IssueTypeResponse>> UpdateAsync(int id, UpdateIssueTypeRequest request)
    {
        var issueType = await _db.IssueTypes
            .FirstOrDefaultAsync(it => it.IssueTypeId == id && !it.IsDeleted);

        if (issueType == null)
        {
            return new ApiResponse<IssueTypeResponse>
            {
                Success = false,
                Message = $"Không tìm thấy loại sự cố với ID '{id}'."
            };
        }

        // 1. Validate TypeCode mới (nếu thay đổi)
        if (!string.IsNullOrWhiteSpace(request.TypeCode))
        {
            var upperCode = request.TypeCode.ToUpperInvariant();
            var codeExists = await _db.IssueTypes
                .AnyAsync(it => it.TypeCode == upperCode && it.IssueTypeId != id);

            if (codeExists)
            {
                return new ApiResponse<IssueTypeResponse>
                {
                    Success = false,
                    Message = $"Mã loại sự cố '{request.TypeCode}' đã tồn tại."
                };
            }

            issueType.TypeCode = request.TypeCode.ToUpperInvariant();
        }

        // 2. Validate ParentIssueTypeId (nếu thay đổi)
        if (request.ParentIssueTypeId.HasValue)
        {
            // Không cho phép parent = chính nó
            if (request.ParentIssueTypeId.Value == id)
            {
                return new ApiResponse<IssueTypeResponse>
                {
                    Success = false,
                    Message = "Loại sự cố cha không thể là chính nó."
                };
            }

            // Kiểm tra parent tồn tại
            var parentExists = await _db.IssueTypes
                .AnyAsync(it => it.IssueTypeId == request.ParentIssueTypeId.Value && !it.IsDeleted);

            if (!parentExists)
            {
                return new ApiResponse<IssueTypeResponse>
                {
                    Success = false,
                    Message = $"Loại sự cố cha với ID '{request.ParentIssueTypeId}' không tồn tại."
                };
            }

            // Kiểm tra không tạo vòng lặp: parent không được là descendant của chính nó
            var isCircular = await CheckCircularReferenceAsync(id, request.ParentIssueTypeId.Value);
            if (isCircular)
            {
                return new ApiResponse<IssueTypeResponse>
                {
                    Success = false,
                    Message = "Không thể đặt loại sự cố này làm cha vì sẽ tạo thành vòng lặp phân cấp."
                };
            }

            issueType.ParentIssueTypeId = request.ParentIssueTypeId.Value;
        }
        else
        {
            // Allow clearing parent (set to null)
            issueType.ParentIssueTypeId = null;
        }

        // 3. Update other fields
        if (!string.IsNullOrWhiteSpace(request.TypeName))
        {
            issueType.TypeName = request.TypeName.Trim();
        }

        if (request.IconUrl != null)
        {
            issueType.IconUrl = request.IconUrl.Trim();
        }

        if (request.Description != null)
        {
            issueType.Description = request.Description.Trim();
        }

        if (request.IsActive.HasValue)
        {
            issueType.IsActive = request.IsActive.Value;
        }

        issueType.UpdatedAtUtc = DateTime.UtcNow;
        issueType.UpdatedBy = _currentUser.UserId;

        await _db.SaveChangesAsync();

        // Return updated response
        var response = await GetIssueTypeResponseAsync(issueType.IssueTypeId);

        return new ApiResponse<IssueTypeResponse>
        {
            Success = true,
            Message = "Cập nhật loại sự cố thành công.",
            Data = response.Data
        };
    }

    /// <inheritdoc />
    public async Task<ApiResponse<bool>> DeleteAsync(int id)
    {
        var issueType = await _db.IssueTypes
            .FirstOrDefaultAsync(it => it.IssueTypeId == id && !it.IsDeleted);

        if (issueType == null)
        {
            return new ApiResponse<bool>
            {
                Success = false,
                Message = $"Không tìm thấy loại sự cố với ID '{id}'."
            };
        }

        // Check nếu có con đang hoạt động
        var hasActiveChildren = await _db.IssueTypes
            .AnyAsync(it => it.ParentIssueTypeId == id && !it.IsDeleted && it.IsActive);

        if (hasActiveChildren)
        {
            return new ApiResponse<bool>
            {
                Success = false,
                Message = "Không thể xóa loại sự cố này vì còn các loại con đang hoạt động. Vui lòng xóa hoặc deactive các loại con trước."
            };
        }

        // Soft delete
        issueType.IsDeleted = true;
        issueType.IsActive = false;
        issueType.UpdatedAtUtc = DateTime.UtcNow;
        issueType.UpdatedBy = _currentUser.UserId;

        await _db.SaveChangesAsync();

        return new ApiResponse<bool>
        {
            Success = true,
            Message = "Xóa loại sự cố thành công.",
            Data = true
        };
    }

    /// <inheritdoc />
    public async Task<ApiResponse<object>> GetLookupAsync(string? mode = "flat")
    {
        var issueTypes = await _db.IssueTypes
            .Where(it => it.IsActive && !it.IsDeleted)
            .OrderBy(it => it.TypeName)
            .Select(it => new IssueTypeLookupResponse
            {
                IssueTypeId = it.IssueTypeId,
                ParentIssueTypeId = it.ParentIssueTypeId,
                TypeCode = it.TypeCode,
                TypeName = it.TypeName,
                IconUrl = it.IconUrl,
                Description = it.Description
            })
            .ToListAsync();

        if (mode?.ToLower() == "tree")
        {
            // Build tree structure: chỉ lấy root items (ParentIssueTypeId = null)
            var rootItems = issueTypes.Where(it => it.ParentIssueTypeId == null).ToList();
            var result = BuildTree(rootItems, issueTypes);
            return new ApiResponse<object> { Success = true, Data = result };
        }

        return new ApiResponse<object>
        {
            Success = true,
            Data = issueTypes
        };
    }

    /// <summary>
    /// Đệ quy xây dựng cây phân cấp từ danh sách phẳng.
    /// </summary>
    private List<IssueTypeTreeResponse> BuildTree(
        List<IssueTypeLookupResponse> rootItems,
        List<IssueTypeLookupResponse> allItems)
    {
        var result = new List<IssueTypeTreeResponse>();

        foreach (var item in rootItems)
        {
            var node = new IssueTypeTreeResponse
            {
                IssueTypeId = item.IssueTypeId,
                TypeCode = item.TypeCode,
                TypeName = item.TypeName,
                IconUrl = item.IconUrl,
                Description = item.Description
            };

            // Tìm các con trực tiếp
            var children = allItems.Where(it => it.ParentIssueTypeId == item.IssueTypeId).ToList();
            if (children.Any())
            {
                node.Children = BuildTree(children, allItems);
            }

            result.Add(node);
        }

        return result;
    }

    // ==================== Private Helper Methods ====================

    /// <summary>
    /// Lấy chi tiết IssueType với navigation properties đã được load.
    /// </summary>
    private async Task<ApiResponse<IssueTypeResponse>> GetIssueTypeResponseAsync(int id)
    {
        var issueType = await _db.IssueTypes
            .Include(it => it.ParentIssueType)
            .Include(it => it.SubIssueTypes.Where(s => !s.IsDeleted))
            .Include(it => it.SlaPolicies)
            .FirstOrDefaultAsync(it => it.IssueTypeId == id && !it.IsDeleted);

        if (issueType == null)
        {
            return new ApiResponse<IssueTypeResponse>
            {
                Success = false,
                Message = $"Không tìm thấy loại sự cố với ID '{id}'."
            };
        }

        return new ApiResponse<IssueTypeResponse>
        {
            Success = true,
            Data = MapToResponse(issueType)
        };
    }

    /// <summary>
    /// Map entity sang response DTO.
    /// </summary>
    private static IssueTypeResponse MapToResponse(IssueType entity)
    {
        string? slaSummary = null;
        if (entity.SlaPolicies != null && entity.SlaPolicies.Any())
        {
            if (entity.SlaPolicies.Count == 1)
            {
                var policy = entity.SlaPolicies.First();
                if (policy.ResolutionMinutes > 0)
                {
                    if (policy.ResolutionMinutes >= 1440)
                        slaSummary = $"{policy.ResolutionMinutes / 1440}-Day Resolution";
                    else if (policy.ResolutionMinutes >= 60)
                        slaSummary = $"{policy.ResolutionMinutes / 60}-Hour Resolution";
                    else
                        slaSummary = $"{policy.ResolutionMinutes}-Min Resolution";
                }
            }
            else
            {
                slaSummary = "Multiple SLAs";
            }
        }

        return new IssueTypeResponse
        {
            IssueTypeId = entity.IssueTypeId,
            ParentIssueTypeId = entity.ParentIssueTypeId,
            TypeCode = entity.TypeCode,
            TypeName = entity.TypeName,
            IconUrl = entity.IconUrl,
            Description = entity.Description,
            SlaPolicySummary = slaSummary,
            IsActive = entity.IsActive,
            CreatedAt = entity.CreatedAtUtc,
            CreatedBy = entity.CreatedBy,
            UpdatedAt = entity.UpdatedAtUtc,
            UpdatedBy = entity.UpdatedBy,
            ParentIssueType = entity.ParentIssueType != null
                ? new IssueTypeSummary
                {
                    IssueTypeId = entity.ParentIssueType.IssueTypeId,
                    TypeCode = entity.ParentIssueType.TypeCode,
                    TypeName = entity.ParentIssueType.TypeName,
                    IconUrl = entity.ParentIssueType.IconUrl,
                    SlaPolicySummary = slaSummary,
                    IsActive = entity.ParentIssueType.IsActive
                }
                : null,
            SubIssueTypes = entity.SubIssueTypes
                .Select(s => new IssueTypeSummary
                {
                    IssueTypeId = s.IssueTypeId,
                    TypeCode = s.TypeCode,
                    TypeName = s.TypeName,
                    IconUrl = s.IconUrl,
                    IsActive = s.IsActive
                })
                .ToList()
        };
    }

    /// <summary>
    /// Kiểm tra xem việc đặt parentId làm cha của childId có tạo thành vòng lặp không.
    /// </summary>
    private async Task<bool> CheckCircularReferenceAsync(int childId, int parentId)
    {
        var currentId = parentId;
        var visited = new HashSet<int> { childId }; // Bắt đầu từ node con để tránh infinite loop

        while (true)
        {
            if (visited.Contains(currentId))
            {
                // Phát hiện vòng lặp
                return true;
            }

            var parent = await _db.IssueTypes
                .Where(it => it.IssueTypeId == currentId && !it.IsDeleted)
                .Select(it => it.ParentIssueTypeId)
                .FirstOrDefaultAsync();

            if (!parent.HasValue)
            {
                // Đã đến root, không có vòng lặp
                return false;
            }

            visited.Add(currentId);
            currentId = parent.Value;
        }
    }
}
