using Microsoft.EntityFrameworkCore;
using UrbanInfraSystem.Application.DTOs.IssueAssignments;
using UrbanInfraSystem.Application.Interfaces;
using UrbanInfraSystem.Domain.Entities;
using UrbanInfraSystem.Infrastructure.Persistence;

namespace UrbanInfraSystem.Infrastructure.Services;

public class IssueAssignmentService : IIssueAssignmentService
{
    private readonly AppDbContext _context;

    public IssueAssignmentService(AppDbContext context) => _context = context;

    public async Task<IReadOnlyList<IssueAssignmentResponse>?> GetHistoryAsync(
        long issueId,
        CancellationToken cancellationToken = default)
    {
        if (!await _context.Issues.AnyAsync(x => x.IssueId == issueId, cancellationToken)) return null;
        var items = await _context.IssueAssignments
            .Include(x => x.Department)
            .Where(x => x.IssueId == issueId)
            .OrderByDescending(x => x.AssignedAt)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
        return items.Select(Map).ToList();
    }

    public async Task<IssueAssignmentResponse> ReassignAsync(
        long issueId,
        ReassignIssueRequest request,
        string actorUserId,
        bool isAdmin,
        CancellationToken cancellationToken = default)
    {
        var issue = await _context.Issues.FirstOrDefaultAsync(x => x.IssueId == issueId, cancellationToken)
            ?? throw new KeyNotFoundException($"Không tìm thấy sự cố có ID = {issueId}.");
        var department = await _context.Departments.FirstOrDefaultAsync(
            x => x.DepartmentId == request.DepartmentId && x.IsActive, cancellationToken)
            ?? throw new ArgumentException($"Đơn vị có ID = {request.DepartmentId} không tồn tại hoặc đã ngừng hoạt động.");

        var current = await _context.IssueAssignments
            .Include(x => x.Department)
            .FirstOrDefaultAsync(x => x.IssueId == issueId && x.IsCurrent, cancellationToken);
        if (current?.DepartmentId == request.DepartmentId)
            throw new InvalidOperationException("Sự cố đã được phân công cho đơn vị này.");

        if (!isAdmin)
        {
            if (current is null || !await _context.DepartmentMembers.AnyAsync(
                    x => x.DepartmentId == current.DepartmentId && x.UserId == actorUserId && x.IsActive && x.IsManager,
                    cancellationToken))
                throw new UnauthorizedAccessException("Chỉ quản lý đơn vị đang phụ trách mới được chuyển sự cố.");
        }

        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        var now = DateTime.UtcNow;
        if (current is not null)
        {
            current.IsCurrent = false;
            current.EndedAt = now;
        }

        var assignment = new IssueAssignment
        {
            IssueId = issueId,
            DepartmentId = department.DepartmentId,
            AssignmentMethod = "MANUAL",
            AssignmentNote = Clean(request.Note),
            AssignedAt = now,
            IsCurrent = true
        };
        _context.IssueAssignments.Add(assignment);
        _context.IssueUpdates.Add(new IssueUpdate
        {
            IssueId = issueId,
            CreatedBy = actorUserId,
            FromStatusId = issue.StatusId,
            ToStatusId = issue.StatusId,
            Note = $"Đã chuyển đơn vị phụ trách từ '{current?.Department.DepartmentName ?? "Chưa phân công"}' sang '{department.DepartmentName}'." +
                   (string.IsNullOrWhiteSpace(request.Note) ? string.Empty : $" Lý do: {request.Note.Trim()}"),
            IsSystemGenerated = false,
            CreatedAt = now
        });

        // Create automatic Notifications for Assignment
        var deptMemberIds = await _context.DepartmentMembers
            .Where(x => x.DepartmentId == department.DepartmentId && x.IsActive)
            .Select(x => x.UserId)
            .ToListAsync(cancellationToken);

        var assignNotifyUsers = deptMemberIds.Concat(new[] { issue.ReporterId })
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Distinct()
            .ToList();

        foreach (var uid in assignNotifyUsers)
        {
            _context.Notifications.Add(new Notification
            {
                UserId = uid,
                Title = $"Phân công sự cố #{issueId}",
                Message = $"Sự cố #{issueId} đã được phân công xử lý cho đơn vị '{department.DepartmentName}'.",
                NotificationType = "ASSIGNMENT",
                IssueId = issueId,
                IsRead = false,
                CreatedAt = now
            });
        }

        _context.AuditLogs.Add(new AuditLog
        {
            ActorUserId = actorUserId,
            Action = "Reassign",
            EntityName = "Issues",
            EntityId = issueId.ToString(),
            OccurredAt = now
        });

        await _context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        assignment.Department = department;
        return Map(assignment);
    }

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static IssueAssignmentResponse Map(IssueAssignment x) => new()
    {
        AssignmentId = x.AssignmentId,
        IssueId = x.IssueId,
        DepartmentId = x.DepartmentId,
        DepartmentCode = x.Department.DepartmentCode,
        DepartmentName = x.Department.DepartmentName,
        RoutingRuleId = x.RoutingRuleId,
        AssignmentMethod = x.AssignmentMethod,
        AssignmentNote = x.AssignmentNote,
        AssignedAt = x.AssignedAt,
        AcceptedAt = x.AcceptedAt,
        EndedAt = x.EndedAt,
        IsCurrent = x.IsCurrent
    };
}
