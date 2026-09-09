using CourseCore.Api.Modules.AuditLogs.Application.Constants;
using CourseCore.Api.Modules.Media.Application.UseCases;
using CourseCore.Api.Modules.Media.Domain.Entities;
using CourseCore.Api.Modules.Media.Domain.Enums;
using CourseCore.Api.Shared.Application.Exceptions;
using CourseCore.Api.Tests.TestDoubles;

namespace CourseCore.Api.Tests.Application.Media;

public class ActivateVideoUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_WhenVideoExists_ShouldMarkAsActiveAndRecordAuditLog()
    {
        var videos = new FakeVideoRepository();
        var video = Video.Create(
            Guid.NewGuid(), "Video", "Description", VideoStorageProvider.Local, "videos/video.mp4", 100, 0);
        video.MarkAsUnlisted();
        videos.Videos.Add(video);
        var auditLogs = new FakeAuditLogService();
        var useCase = new ActivateVideoUseCase(videos, new FakeUnitOfWork(), auditLogs);

        var output = await useCase.ExecuteAsync(video.Id);

        Assert.Equal("Active", output.Visibility);
        var auditLog = Assert.Single(auditLogs.Entries, e => e.Action == AuditLogActionNames.VideoActivated);
        Assert.Equal("Video", auditLog.Metadata["displayName"]);
    }

    [Fact]
    public async Task ExecuteAsync_WhenVideoDoesNotExist_ShouldThrowNotFoundException()
    {
        var useCase = new ActivateVideoUseCase(new FakeVideoRepository(), new FakeUnitOfWork(), new FakeAuditLogService());

        await Assert.ThrowsAsync<NotFoundException>(() => useCase.ExecuteAsync(Guid.NewGuid()));
    }
}
