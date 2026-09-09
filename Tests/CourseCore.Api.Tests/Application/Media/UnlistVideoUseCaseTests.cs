using CourseCore.Api.Modules.AuditLogs.Application.Constants;
using CourseCore.Api.Modules.Media.Application.UseCases;
using CourseCore.Api.Modules.Media.Domain.Entities;
using CourseCore.Api.Modules.Media.Domain.Enums;
using CourseCore.Api.Shared.Application.Exceptions;
using CourseCore.Api.Tests.TestDoubles;

namespace CourseCore.Api.Tests.Application.Media;

public class UnlistVideoUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_WhenVideoExists_ShouldMarkAsUnlistedAndRecordAuditLog()
    {
        var videos = new FakeVideoRepository();
        var video = Video.Create(
            Guid.NewGuid(), "Video", "Description", VideoStorageProvider.Local, "videos/video.mp4", 100, 0);
        videos.Videos.Add(video);
        var auditLogs = new FakeAuditLogService();
        var useCase = new UnlistVideoUseCase(videos, new FakeUnitOfWork(), auditLogs);

        var output = await useCase.ExecuteAsync(video.Id);

        Assert.Equal("Unlisted", output.Visibility);
        var auditLog = Assert.Single(auditLogs.Entries, e => e.Action == AuditLogActionNames.VideoUnlisted);
        Assert.Equal("Video", auditLog.Metadata["displayName"]);
    }

    [Fact]
    public async Task ExecuteAsync_WhenVideoDoesNotExist_ShouldThrowNotFoundException()
    {
        var useCase = new UnlistVideoUseCase(new FakeVideoRepository(), new FakeUnitOfWork(), new FakeAuditLogService());

        await Assert.ThrowsAsync<NotFoundException>(() => useCase.ExecuteAsync(Guid.NewGuid()));
    }
}
