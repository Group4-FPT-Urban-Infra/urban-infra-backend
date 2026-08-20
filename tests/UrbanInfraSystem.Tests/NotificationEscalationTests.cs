using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using UrbanInfraSystem.API.Controllers;
using UrbanInfraSystem.Application.Interfaces;
using UrbanInfraSystem.Domain.Entities;
using UrbanInfraSystem.Infrastructure.Persistence;
using UrbanInfraSystem.Infrastructure.Services;
using UrbanInfraSystem.Infrastructure.Services.Elaboration;
using Xunit;

namespace UrbanInfraSystem.Tests;

public class NotificationEscalationTests
{
    private AppDbContext GetInMemoryDbContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task EscalationEvent_ShouldAutomaticallyCreateNotification_And_GetUnread_ReturnsNotificationForUser()
    {
        // 1. Arrange
        var dbName = Guid.NewGuid().ToString();
        using var db = GetInMemoryDbContext(dbName);

        var targetUserId = "user-staff-123";
        var issueId = 1001L;

        // Seed Issue
        var issue = new Issue
        {
            IssueId = issueId,
            PublicCode = "ISS-TEST-001",
            ReporterId = "user-citizen-999",
            IssueTypeId = 1,
            AreaId = 1,
            PriorityId = 1,
            StatusId = 1,
            Title = "Đèn đường hỏng",
            Description = "Cột đèn số 5 không sáng",
            ReportedAt = DateTime.UtcNow.AddDays(-2)
        };
        db.Issues.Add(issue);

        // Seed Department and Member
        var dept = new Department
        {
            DepartmentId = 5,
            DepartmentCode = "DEPT_LIGHT",
            DepartmentName = "Phòng Chiếu Sáng",
            IsActive = true
        };
        db.Departments.Add(dept);

        db.DepartmentMembers.Add(new DepartmentMember
        {
            DepartmentId = 5,
            UserId = targetUserId,
            IsManager = true,
            IsActive = true
        });

        // Seed SlaPolicy & EscalationRule
        var policyId = Guid.NewGuid();
        db.SlaPolicies.Add(new SlaPolicy
        {
            Id = policyId,
            IssueTypeId = 1,
            PriorityId = 1,
            FirstResponseMinutes = 120,
            ResolutionMinutes = 1440
        });

        var ruleId = Guid.NewGuid();
        db.EscalationRules.Add(new EscalationRule
        {
            Id = ruleId,
            SlaPolicyId = policyId,
            EscalationLevel = 1,
            OverdueMinutes = 30,
            TargetDepartmentId = 5,
            IsActive = true
        });

        // Seed overdue IssueSla
        db.IssueSlas.Add(new IssueSla
        {
            Id = 1,
            IssueId = issueId,
            SlaPolicyId = policyId,
            ResolutionDueAt = DateTime.UtcNow.AddMinutes(-40), // Overdue by 40 mins
            ResolvedAt = null
        });

        await db.SaveChangesAsync();

        var logger = NullLogger<EscalationProcessor>.Instance;
        var processor = new EscalationProcessor(db, logger);

        // 2. Act: Trigger SLA Breach Processor
        await processor.ProcessOnceAsync();

        // 3. Assert EscalationEvent created
        var escalationEvents = await db.EscalationEvents.ToListAsync();
        Assert.Single(escalationEvents);
        Assert.Equal(issueId, escalationEvents[0].IssueId);

        // 4. Assert Notification automatically created in DB
        var notifications = await db.Notifications.Where(n => n.UserId == targetUserId).ToListAsync();
        Assert.NotEmpty(notifications);
        var notify = notifications.First();
        Assert.Equal("ESCALATION", notify.NotificationType);
        Assert.Equal(issueId, notify.IssueId);
        Assert.False(notify.IsRead);

        // 5. Test GET /api/notifications?user_id=user-staff-123 returns unread list according to target user
        var notificationService = new NotificationService(db);
        var mockCurrentUser = new MockCurrentUserService(targetUserId);
        var controller = new NotificationsController(notificationService, mockCurrentUser);

        var actionResult = await controller.GetUnread(userId: targetUserId, default);
        var okResult = Assert.IsType<Microsoft.AspNetCore.Mvc.OkObjectResult>(actionResult.Result);
        var unreadList = Assert.IsAssignableFrom<System.Collections.Generic.IReadOnlyList<UrbanInfraSystem.Application.DTOs.Notifications.NotificationResponse>>(okResult.Value);

        Assert.Single(unreadList);
        Assert.Equal(targetUserId, unreadList[0].UserId);
        Assert.Equal(issueId, unreadList[0].IssueId);
        Assert.Equal("ESCALATION", unreadList[0].NotificationType);
    }

    private class MockCurrentUserService : ICurrentUserService
    {
        public string? UserId { get; }
        public string? Email => "test@example.com";
        public IReadOnlyList<string> Roles => new[] { "Admin" };
        public bool IsAuthenticated => true;

        public MockCurrentUserService(string userId)
        {
            UserId = userId;
        }

        public bool IsInRole(string role) => true;
    }
}
