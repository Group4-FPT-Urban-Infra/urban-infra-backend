using Microsoft.EntityFrameworkCore;
using UrbanInfraSystem.Application.DTOs.IssueAssignmentMembers;
using UrbanInfraSystem.Application.Interfaces;
using UrbanInfraSystem.Domain.Entities;
using UrbanInfraSystem.Infrastructure.Identity;
using UrbanInfraSystem.Infrastructure.Persistence;

namespace UrbanInfraSystem.Infrastructure.Services;

public class IssueAssignmentMemberService : IIssueAssignmentMemberService
{
    private readonly AppDbContext _context;

    public IssueAssignmentMemberService(AppDbContext context) => _context = context;

    public async Task<IReadOnlyList<IssueAssignmentMemberResponse>?> GetByAssignmentAsync(
        long assignmentId,
        CancellationToken cancellationToken = default)
    {
        if (!await _context.IssueAssignments.AnyAsync(x => x.AssignmentId == assignmentId, cancellationToken))
            return null;

        var items = await _context.IssueAssignmentMembers
            .Include(x => x.Assignment)
            .Where(x => x.AssignmentId == assignmentId)
            .OrderByDescending(x => x.AssignedAt)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var userIds = items.Select(x => x.UserId).Concat(items.Select(x => x.AssignedBy)).Distinct();
        var users = await _context.Users
            .Where(u => userIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, cancellationToken);

        return items.Select(x => Map(x, users)).ToList();
    }

    public async Task<IssueAssignmentMemberResponse?> GetMemberDetailAsync(
        long memberId,
        CancellationToken cancellationToken = default)
    {
        var member = await _context.IssueAssignmentMembers
            .Include(x => x.Assignment)
            .FirstOrDefaultAsync(x => x.MemberId == memberId, cancellationToken);

        if (member is null) return null;

        var userIds = new[] { member.UserId, member.AssignedBy }.Distinct();
        var users = await _context.Users
            .Where(u => userIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, cancellationToken);

        return Map(member, users);
    }

    public async Task<IssueAssignmentMemberResponse> AssignMemberAsync(
        long assignmentId,
        AssignMemberRequest request,
        string assignedBy,
        CancellationToken cancellationToken = default)
    {
        var assignment = await _context.IssueAssignments
            .Include(x => x.Department)
            .FirstOrDefaultAsync(x => x.AssignmentId == assignmentId, cancellationToken)
            ?? throw new KeyNotFoundException($"Khong tim thay phan cong co ID = {assignmentId}.");

        if (!assignment.IsCurrent)
            throw new InvalidOperationException("Khong the gan nhan vien vao phan cong da ket thuc.");

        var user = await _context.Users.FindAsync(new object[] { request.UserId }, cancellationToken)
            ?? throw new KeyNotFoundException($"Khong tim thay nguoi dung co ID = {request.UserId}.");

        var isStaffInDept = await _context.DepartmentMembers.AnyAsync(
            x => x.DepartmentId == assignment.DepartmentId && x.UserId == request.UserId && x.IsActive,
            cancellationToken);

        if (!isStaffInDept)
            throw new InvalidOperationException("Nguoi dung khong phai la nhan vien dang hoat dong cua don vi nay.");

        var existingMember = await _context.IssueAssignmentMembers
            .FirstOrDefaultAsync(x => x.AssignmentId == assignmentId && x.UserId == request.UserId, cancellationToken);

        if (existingMember is not null)
            throw new InvalidOperationException("Nhan vien nay da duoc gan vao phan cong.");

        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        var now = DateTime.UtcNow;

        var member = new IssueAssignmentMember
        {
            AssignmentId = assignmentId,
            UserId = request.UserId,
            AssignedBy = assignedBy,
            AssignedAt = now,
            Status = AssignmentMemberStatus.Pending,
            Note = Clean(request.Note)
        };

        _context.IssueAssignmentMembers.Add(member);

        _context.IssueUpdates.Add(new IssueUpdate
        {
            IssueId = assignment.IssueId,
            CreatedBy = assignedBy,
            Note = $"Da gan nhan vien '{user.FullName}' vao phan cong." +
                   (string.IsNullOrWhiteSpace(request.Note) ? string.Empty : $" Ghi chu: {request.Note.Trim()}"),
            IsSystemGenerated = false,
            CreatedAt = now
        });

        _context.Notifications.Add(new Notification
        {
            UserId = request.UserId,
            Title = "Ban duoc gan xu ly su co",
            Message = $"Ban da duoc gan xu ly mot su co trong phan cong cua don vi '{assignment.Department.DepartmentName}'.",
            NotificationType = "ASSIGNMENT_MEMBER",
            IssueId = assignment.IssueId,
            IsRead = false,
            CreatedAt = now
        });

        await _context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        var users = (await _context.Users
            .Where(u => u.Id == request.UserId || u.Id == assignedBy)
            .ToDictionaryAsync(u => u.Id, cancellationToken));

        member.Assignment = assignment;
        return Map(member, users);
    }

    public async Task<IssueAssignmentMemberResponse> UpdateMemberStatusAsync(
        long memberId,
        UpdateMemberStatusRequest request,
        string userId,
        CancellationToken cancellationToken = default)
    {
        var member = await _context.IssueAssignmentMembers
            .Include(x => x.Assignment)
            .FirstOrDefaultAsync(x => x.MemberId == memberId, cancellationToken)
            ?? throw new KeyNotFoundException($"Khong tim thay thanh vien co ID = {memberId}.");

        var validStatuses = new[] { AssignmentMemberStatus.Pending, AssignmentMemberStatus.Accepted, AssignmentMemberStatus.Rejected, AssignmentMemberStatus.Completed };
        if (!validStatuses.Contains(request.Status.ToUpperInvariant()))
            throw new ArgumentException($"Trang thai '{request.Status}' khong hop le. Cac trang thai hop le: {string.Join(", ", validStatuses)}.");

        var newStatus = request.Status.ToUpperInvariant();
        var now = DateTime.UtcNow;

        if (newStatus == AssignmentMemberStatus.Accepted)
        {
            if (member.UserId != userId)
                throw new UnauthorizedAccessException("Chi nhan vien duoc gan moi co the chap nhan.");

            if (member.Status != AssignmentMemberStatus.Pending)
                throw new InvalidOperationException("Chi co the chap nhan khi dang o trang thai PENDING.");

            member.AcceptedAt = now;
        }
        else if (newStatus == AssignmentMemberStatus.Rejected)
        {
            if (member.UserId != userId)
                throw new UnauthorizedAccessException("Chi nhan vien duoc gan moi co the tu choi.");

            if (member.Status != AssignmentMemberStatus.Pending)
                throw new InvalidOperationException("Chi co the tu choi khi dang o trang thai PENDING.");

            member.EndedAt = now;
        }
        else if (newStatus == AssignmentMemberStatus.Completed)
        {
            if (member.UserId != userId)
                throw new UnauthorizedAccessException("Chi nhan vien duoc gan moi co the danh dau hoan thanh.");

            if (member.Status != AssignmentMemberStatus.Accepted)
                throw new InvalidOperationException("Chi co the hoan thanh khi dang o trang thai ACCEPTED.");

            member.EndedAt = now;
        }

        member.Status = newStatus;
        if (!string.IsNullOrWhiteSpace(request.Note))
            member.Note = request.Note;

        await _context.SaveChangesAsync(cancellationToken);

        var users = (await _context.Users
            .Where(u => u.Id == member.UserId || u.Id == member.AssignedBy)
            .ToDictionaryAsync(u => u.Id, cancellationToken));

        return Map(member, users);
    }

    public async Task RemoveMemberAsync(
        long memberId,
        string removedBy,
        CancellationToken cancellationToken = default)
    {
        var member = await _context.IssueAssignmentMembers
            .Include(x => x.Assignment)
            .FirstOrDefaultAsync(x => x.MemberId == memberId, cancellationToken)
            ?? throw new KeyNotFoundException($"Khong tim thay thanh vien co ID = {memberId}.");

        if (member.Status == AssignmentMemberStatus.Accepted)
            throw new InvalidOperationException("Khong the xoa nhan vien dang xu ly. Hay yeu cau ho hoan thanh hoac tu choi truoc.");

        _context.IssueAssignmentMembers.Remove(member);
        await _context.SaveChangesAsync(cancellationToken);
    }

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static IssueAssignmentMemberResponse Map(IssueAssignmentMember x, Dictionary<string, ApplicationUser> users) => new()
    {
        MemberId = x.MemberId,
        AssignmentId = x.AssignmentId,
        UserId = x.UserId,
        UserFullName = users.TryGetValue(x.UserId, out var user) ? user.FullName : string.Empty,
        UserEmail = users.TryGetValue(x.UserId, out var u1) ? u1.Email ?? string.Empty : string.Empty,
        AssignedBy = x.AssignedBy,
        AssignorFullName = users.TryGetValue(x.AssignedBy, out var assignor) ? assignor.FullName : string.Empty,
        AssignedAt = x.AssignedAt,
        AcceptedAt = x.AcceptedAt,
        EndedAt = x.EndedAt,
        Status = x.Status,
        Note = x.Note
    };
}
