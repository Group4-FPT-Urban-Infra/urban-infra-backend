using UrbanInfraSystem.Domain.Entities;
using Xunit;

namespace UrbanInfraSystem.Tests;

public class IssueAttachmentTests
{
    [Fact]
    public async Task GetIssueByIdAsync_MapsAllAttachmentMetadata()
    {
        await using var db = TestDb.Create();
        IssueTests.SeedLookups(db);
        db.Issues.Add(IssueTests.NewIssue(11, "ISS-011", "Đèn đường hỏng"));
        db.IssueUpdates.Add(new IssueUpdate
        {
            Id = 91,
            IssueId = 11,
            CreatedBy = "citizen-test",
            ToStatusId = 1,
            CreatedAt = DateTime.UtcNow
        });
        var createdAt = new DateTime(2026, 8, 18, 1, 2, 3, DateTimeKind.Utc);
        db.IssueAttachments.Add(new IssueAttachment(91)
        {
            Id = 101,
            IssueId = 11,
            UploadedBy = "citizen-test",
            Kind = "image",
            FileUrl = "/uploads/issues/damage.jpg",
            ThumbnailUrl = "/uploads/issues/damage-thumb.jpg",
            MimeType = "image/jpeg",
            FileSizeBytes = 2048,
            WidthPx = 1280,
            HeightPx = 720,
            CreatedAt = createdAt
        });
        await db.SaveChangesAsync();

        var result = await IssueTests.CreateService(db).GetIssueByIdAsync(11);

        var attachment = Assert.Single(result.Data!.Attachments);
        Assert.Equal(101, attachment.Id);
        Assert.Equal("image", attachment.Kind);
        Assert.Equal("/uploads/issues/damage.jpg", attachment.FileUrl);
        Assert.Equal("/uploads/issues/damage-thumb.jpg", attachment.ThumbnailUrl);
        Assert.Equal("image/jpeg", attachment.MimeType);
        Assert.Equal(2048, attachment.FileSizeBytes);
        Assert.Equal(1280, attachment.WidthPx);
        Assert.Equal(720, attachment.HeightPx);
        Assert.Equal(createdAt, attachment.CreatedAt);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_RejectsMissingOrInvalidIssueUpdate(long updateId)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new IssueAttachment(updateId));
    }

    [Fact]
    public async Task GetIssueByIdAsync_ReturnsEmptyAttachments_WhenIssueHasNoFiles()
    {
        await using var db = TestDb.Create();
        IssueTests.SeedLookups(db);
        db.Issues.Add(IssueTests.NewIssue(12, "ISS-012", "Không có ảnh"));
        await db.SaveChangesAsync();

        var result = await IssueTests.CreateService(db).GetIssueByIdAsync(12);

        Assert.True(result.Success);
        Assert.Empty(result.Data!.Attachments);
    }
}
