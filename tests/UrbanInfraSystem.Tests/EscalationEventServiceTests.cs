using Microsoft.EntityFrameworkCore;
using UrbanInfraSystem.Domain.Entities;
using UrbanInfraSystem.Infrastructure.Services;
using Xunit;

namespace UrbanInfraSystem.Tests;

public class EscalationEventServiceTests
{
    [Fact]
    public async Task GetByIssueAsync_ReturnsEvents_WhenIssueExists()
    {
        await using var db = TestDb.Create();
        db.Issues.Add(new Issue { IssueId = 1, Title = "Issue 1", Description = "Test Description", PublicCode = "ISS-1", ReporterId = "user1" });
        db.EscalationEvents.Add(new EscalationEvent
        {
            Id = Guid.NewGuid(),
            IssueId = 1,
            EventStatus = "Pending",
            TriggeredAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var service = new EscalationEventService(db);
        var result = await service.GetByIssueAsync(1);

        Assert.NotEmpty(result);
        Assert.Equal(1, result[0].IssueId);
    }

    [Fact]
    public async Task AcknowledgeAsync_SuccessfullyAcknowledgesEvent()
    {
        await using var db = TestDb.Create();
        var eventId = Guid.NewGuid();
        db.EscalationEvents.Add(new EscalationEvent
        {
            Id = eventId,
            IssueId = 1,
            EventStatus = "Pending",
            TriggeredAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var service = new EscalationEventService(db);
        var result = await service.AcknowledgeAsync(eventId, "user1");

        Assert.NotNull(result);
        Assert.Equal("user1", result.AcknowledgedBy);
        Assert.Equal("Acknowledged", result.EventStatus);
        Assert.NotNull(result.AcknowledgedAt);

        var eventInDb = await db.EscalationEvents.FirstOrDefaultAsync(e => e.Id == eventId);
        Assert.NotNull(eventInDb);
        Assert.Equal("user1", eventInDb.AcknowledgedBy);
    }

    [Fact]
    public async Task AcknowledgeAsync_ThrowsException_WhenAlreadyAcknowledged()
    {
        await using var db = TestDb.Create();
        var eventId = Guid.NewGuid();
        db.EscalationEvents.Add(new EscalationEvent
        {
            Id = eventId,
            IssueId = 1,
            EventStatus = "Acknowledged",
            AcknowledgedBy = "user1",
            TriggeredAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var service = new EscalationEventService(db);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => service.AcknowledgeAsync(eventId, "user2"));
        Assert.Contains("already acknowledged", exception.Message);
    }
}
