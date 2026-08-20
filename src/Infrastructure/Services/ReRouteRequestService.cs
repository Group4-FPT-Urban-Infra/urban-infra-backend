using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UrbanInfraSystem.Application.DTOs.IssueAssignments;
using UrbanInfraSystem.Application.DTOs.ReRouteRequests;
using UrbanInfraSystem.Application.Interfaces;
using UrbanInfraSystem.Domain.Entities;
using UrbanInfraSystem.Infrastructure.Persistence;

namespace UrbanInfraSystem.Infrastructure.Services;

public class ReRouteRequestService : IReRouteRequestService
{
    private readonly AppDbContext _context;
    private readonly INotificationService _notificationService;

    public ReRouteRequestService(AppDbContext context, INotificationService notificationService)
    {
        _context = context;
        _notificationService = notificationService;
    }

    public async Task<ReRouteRequestDto> CreateRequestAsync(long issueId, ReassignIssueRequest request, string actorUserId, bool isAdmin, CancellationToken cancellationToken = default)
    {
        var issue = await _context.Issues.FirstOrDefaultAsync(x => x.IssueId == issueId, cancellationToken)
            ?? throw new KeyNotFoundException($"Không tìm thấy sự cố có ID = {issueId}.");

        var targetDepartment = await _context.Departments.FirstOrDefaultAsync(
            x => x.DepartmentId == request.DepartmentId && x.IsActive, cancellationToken)
            ?? throw new ArgumentException($"Đơn vị có ID = {request.DepartmentId} không tồn tại hoặc đã ngừng hoạt động.");

        var currentAssignment = await _context.IssueAssignments
            .Include(x => x.Department)
            .FirstOrDefaultAsync(x => x.IssueId == issueId && x.IsCurrent, cancellationToken);
            
        if (currentAssignment == null)
            throw new InvalidOperationException("Sự cố hiện chưa được phân công cho đơn vị nào.");

        if (currentAssignment.DepartmentId == request.DepartmentId)
            throw new InvalidOperationException("Sự cố đã được phân công cho đơn vị này.");

        if (!isAdmin)
        {
            if (!await _context.DepartmentMembers.AnyAsync(
                    x => x.DepartmentId == currentAssignment.DepartmentId && x.UserId == actorUserId && x.IsActive && x.IsManager,
                    cancellationToken))
                throw new UnauthorizedAccessException("Chỉ quản lý đơn vị đang phụ trách mới được yêu cầu chuyển sự cố.");
        }

        // Kiểm tra xem đã có request nào đang Pending cho sự cố này chưa
        var existingRequest = await _context.ReRouteRequests
            .FirstOrDefaultAsync(x => x.IssueId == issueId && x.Status == ReRouteStatus.Pending, cancellationToken);
        if (existingRequest != null)
            throw new InvalidOperationException("Sự cố này đang có một yêu cầu chuyển tiếp chờ xử lý.");

        var reRouteRequest = new ReRouteRequest
        {
            IssueId = issueId,
            CurrentDepartmentId = currentAssignment.DepartmentId,
            TargetDepartmentId = request.DepartmentId,
            Note = request.Note,
            Status = ReRouteStatus.Pending,
            RequestedBy = actorUserId,
            RequestedAt = DateTime.UtcNow
        };

        _context.ReRouteRequests.Add(reRouteRequest);
        await _context.SaveChangesAsync(cancellationToken);

        var targetManagers = await _context.DepartmentMembers
            .Where(x => x.DepartmentId == request.DepartmentId && x.IsActive && x.IsManager)
            .Select(x => x.UserId)
            .ToListAsync(cancellationToken);

        var currentDeptName = currentAssignment.Department.DepartmentName;
        foreach (var managerId in targetManagers)
        {
            await _notificationService.CreateNotificationAsync(new UrbanInfraSystem.Application.DTOs.Notifications.CreateNotificationRequest
            {
                UserId = managerId,
                Title = "Yêu cầu tiếp nhận sự cố",
                Message = $"Đơn vị {currentDeptName} đã gửi yêu cầu chuyển tiếp sự cố #{issueId} sang cho đơn vị của bạn.",
                IssueId = issueId,
                NotificationType = "SYSTEM"
            }, cancellationToken);
        }

        return await MapToDtoAsync(reRouteRequest, cancellationToken);
    }

    public async Task<IReadOnlyList<ReRouteRequestDto>> GetIncomingRequestsAsync(int targetDepartmentId, CancellationToken cancellationToken = default)
    {
        var requests = await _context.ReRouteRequests
            .Include(x => x.CurrentDepartment)
            .Include(x => x.TargetDepartment)
            .Where(x => x.TargetDepartmentId == targetDepartmentId && x.Status == ReRouteStatus.Pending)
            .OrderByDescending(x => x.RequestedAt)
            .ToListAsync(cancellationToken);

        return requests.Select(MapToDto).ToList();
    }

    public async Task<ReRouteRequestDto?> GetPendingRequestByIssueIdAsync(long issueId, CancellationToken cancellationToken = default)
    {
        var request = await _context.ReRouteRequests
            .Include(x => x.CurrentDepartment)
            .Include(x => x.TargetDepartment)
            .FirstOrDefaultAsync(x => x.IssueId == issueId && x.Status == ReRouteStatus.Pending, cancellationToken);

        return request != null ? MapToDto(request) : null;
    }

    public async Task<bool> AcceptRequestAsync(long requestId, string actorUserId, CancellationToken cancellationToken = default)
    {
        var request = await _context.ReRouteRequests
            .Include(x => x.TargetDepartment)
            .FirstOrDefaultAsync(x => x.Id == requestId, cancellationToken)
            ?? throw new KeyNotFoundException($"Không tìm thấy yêu cầu chuyển tiếp có ID = {requestId}.");

        if (request.Status != ReRouteStatus.Pending)
            throw new InvalidOperationException("Yêu cầu này không còn ở trạng thái chờ xử lý.");

        // Kiểm tra quyền: phải là Admin hoặc Manager của target department
        bool isAdmin = await _context.UserRoles
            .Join(_context.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => new { ur.UserId, r.Name })
            .AnyAsync(x => x.UserId == actorUserId && x.Name == "Admin", cancellationToken);

        if (!isAdmin)
        {
            if (!await _context.DepartmentMembers.AnyAsync(
                    x => x.DepartmentId == request.TargetDepartmentId && x.UserId == actorUserId && x.IsActive && x.IsManager,
                    cancellationToken))
                throw new UnauthorizedAccessException("Bạn không có quyền duyệt yêu cầu cho đơn vị này.");
        }

        var currentAssignment = await _context.IssueAssignments
            .Include(x => x.Members)
            .Include(x => x.Department)
            .FirstOrDefaultAsync(x => x.IssueId == request.IssueId && x.IsCurrent, cancellationToken);

        if (currentAssignment == null)
            throw new InvalidOperationException("Không tìm thấy phân công hiện tại cho sự cố này.");

        var issue = await _context.Issues.FirstOrDefaultAsync(x => x.IssueId == request.IssueId, cancellationToken);
        if (issue == null)
            throw new InvalidOperationException("Sự cố không tồn tại.");

        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var now = DateTime.UtcNow;
            
            // Bước 1: Xóa members cũ (nếu có)
            if (currentAssignment.Members.Count > 0)
            {
                var removedMembers = currentAssignment.Members.ToList();
                _context.IssueAssignmentMembers.RemoveRange(removedMembers);
            }

            string oldDeptName = currentAssignment.Department.DepartmentName;

            // Bước 2: Cập nhật assignment hiện tại
            currentAssignment.DepartmentId = request.TargetDepartmentId;
            currentAssignment.AssignmentMethod = "TRANSFER";
            currentAssignment.AcceptedAt = now;

            // Bước 3: Cập nhật trạng thái Request
            request.Status = ReRouteStatus.Accepted;
            request.ProcessedAt = now;
            request.ProcessedBy = actorUserId;

            // Lấy status "NEW" (Mới tiếp nhận) - StatusId = 1
            var newStatus = await _context.IssueStatuses
                .FirstOrDefaultAsync(x => x.StatusId == 1, cancellationToken)
                ?? throw new InvalidOperationException("Không tìm thấy trạng thái 'Mới tiếp nhận'.");

            var previousStatusId = issue.StatusId;
            issue.StatusId = newStatus.StatusId;
            issue.ResolvedAt = null;

            // Ghi log
            _context.IssueUpdates.Add(new IssueUpdate
            {
                IssueId = request.IssueId,
                CreatedBy = actorUserId,
                FromStatusId = previousStatusId,
                ToStatusId = newStatus.StatusId,
                Note = $"Đã chấp nhận chuyển đơn vị phụ trách từ '{oldDeptName}' sang '{request.TargetDepartment.DepartmentName}'.",
                IsSystemGenerated = false,
                CreatedAt = now
            });

            // Create automatic Notifications for Assignment (chuyển qua cho NV target department)
            var deptMemberIds = await _context.DepartmentMembers
                .Where(x => x.DepartmentId == request.TargetDepartmentId && x.IsActive)
                .Select(x => x.UserId)
                .ToListAsync(cancellationToken);

            if (deptMemberIds.Any())
            {
                var notifications = deptMemberIds.Select(userId => new Notification
                {
                    UserId = userId,
                    Title = "Được phân công sự cố mới",
                    Message = $"Sự cố #{issue.IssueId} - {issue.Title} vừa được chuyển tiếp đến đơn vị của bạn.",
                    NotificationType = "ASSIGNMENT",
                    IssueId = issue.IssueId,
                    IsRead = false,
                    CreatedAt = now
                });
                _context.Notifications.AddRange(notifications);
            }

            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            
            var targetDeptName = request.TargetDepartment.DepartmentName;
            await _notificationService.CreateNotificationAsync(new UrbanInfraSystem.Application.DTOs.Notifications.CreateNotificationRequest
            {
                UserId = request.RequestedBy,
                Title = "Yêu cầu chuyển tiếp được chấp nhận",
                Message = $"Đơn vị {targetDeptName} đã chấp nhận tiếp nhận sự cố #{request.IssueId}.",
                IssueId = request.IssueId,
                NotificationType = "SYSTEM"
            }, cancellationToken);

            return true;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<bool> RejectRequestAsync(long requestId, string actorUserId, CancellationToken cancellationToken = default)
    {
        var request = await _context.ReRouteRequests
            .Include(x => x.TargetDepartment)
            .FirstOrDefaultAsync(x => x.Id == requestId, cancellationToken)
            ?? throw new KeyNotFoundException($"Không tìm thấy yêu cầu chuyển tiếp có ID = {requestId}.");

        if (request.Status != ReRouteStatus.Pending)
            throw new InvalidOperationException("Yêu cầu này không còn ở trạng thái chờ xử lý.");

        // Kiểm tra quyền: phải là Admin hoặc Manager của target department
        bool isAdmin = await _context.UserRoles
            .Join(_context.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => new { ur.UserId, r.Name })
            .AnyAsync(x => x.UserId == actorUserId && x.Name == "Admin", cancellationToken);

        if (!isAdmin)
        {
            if (!await _context.DepartmentMembers.AnyAsync(
                    x => x.DepartmentId == request.TargetDepartmentId && x.UserId == actorUserId && x.IsActive && x.IsManager,
                    cancellationToken))
                throw new UnauthorizedAccessException("Bạn không có quyền từ chối yêu cầu cho đơn vị này.");
        }

        request.Status = ReRouteStatus.Rejected;
        request.ProcessedAt = DateTime.UtcNow;
        request.ProcessedBy = actorUserId;

        await _context.SaveChangesAsync(cancellationToken);

        var targetDeptName = request.TargetDepartment.DepartmentName;
        await _notificationService.CreateNotificationAsync(new UrbanInfraSystem.Application.DTOs.Notifications.CreateNotificationRequest
        {
            UserId = request.RequestedBy,
            Title = "Yêu cầu chuyển tiếp bị từ chối",
            Message = $"Đơn vị {targetDeptName} đã từ chối tiếp nhận sự cố #{request.IssueId}.",
            IssueId = request.IssueId,
            NotificationType = "SYSTEM"
        }, cancellationToken);

        return true;
    }

    public async Task<bool> CancelRequestAsync(long requestId, string actorUserId, CancellationToken cancellationToken = default)
    {
        var request = await _context.ReRouteRequests
            .FirstOrDefaultAsync(x => x.Id == requestId, cancellationToken)
            ?? throw new KeyNotFoundException($"Không tìm thấy yêu cầu chuyển tiếp có ID = {requestId}.");

        if (request.Status != ReRouteStatus.Pending)
            throw new InvalidOperationException("Yêu cầu này không còn ở trạng thái chờ xử lý.");

        if (request.RequestedBy != actorUserId)
            throw new UnauthorizedAccessException("Chỉ người tạo yêu cầu mới được hủy.");

        request.Status = ReRouteStatus.Cancelled;
        request.ProcessedAt = DateTime.UtcNow;
        request.ProcessedBy = actorUserId;

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    private async Task<ReRouteRequestDto> MapToDtoAsync(ReRouteRequest entity, CancellationToken cancellationToken)
    {
        var currentDept = await _context.Departments.FindAsync(new object[] { entity.CurrentDepartmentId }, cancellationToken);
        var targetDept = await _context.Departments.FindAsync(new object[] { entity.TargetDepartmentId }, cancellationToken);

        return new ReRouteRequestDto
        {
            Id = entity.Id,
            IssueId = entity.IssueId,
            CurrentDepartmentId = entity.CurrentDepartmentId,
            CurrentDepartmentName = currentDept?.DepartmentName ?? string.Empty,
            TargetDepartmentId = entity.TargetDepartmentId,
            TargetDepartmentName = targetDept?.DepartmentName ?? string.Empty,
            Note = entity.Note,
            Status = entity.Status.ToString(),
            RequestedBy = entity.RequestedBy,
            RequestedAt = entity.RequestedAt
        };
    }

    private ReRouteRequestDto MapToDto(ReRouteRequest entity)
    {
        return new ReRouteRequestDto
        {
            Id = entity.Id,
            IssueId = entity.IssueId,
            CurrentDepartmentId = entity.CurrentDepartmentId,
            CurrentDepartmentName = entity.CurrentDepartment?.DepartmentName ?? string.Empty,
            TargetDepartmentId = entity.TargetDepartmentId,
            TargetDepartmentName = entity.TargetDepartment?.DepartmentName ?? string.Empty,
            Note = entity.Note,
            Status = entity.Status.ToString(),
            RequestedBy = entity.RequestedBy,
            RequestedAt = entity.RequestedAt
        };
    }
}
