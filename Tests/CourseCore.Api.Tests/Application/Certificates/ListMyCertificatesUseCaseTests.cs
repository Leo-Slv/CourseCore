using CourseCore.Api.Modules.Certificates.Application.UseCases;
using CourseCore.Api.Modules.Certificates.Domain.Entities;
using CourseCore.Api.Tests.TestDoubles;

namespace CourseCore.Api.Tests.Application.Certificates;

public class ListMyCertificatesUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_ShouldReturnCertificatesOrderedByIssuedAtDescendingWithCourseTitle()
    {
        var certificates = new FakeCertificateRepository();
        var courses = new FakeCourseRepository();
        var userId = Guid.NewGuid();
        var area = TestEntityFactory.Area();
        var olderCourse = TestEntityFactory.PublishedCourse(area.Id);
        var newerCourse = TestEntityFactory.PublishedCourse(area.Id);
        courses.Courses.Add(olderCourse);
        courses.Courses.Add(newerCourse);

        var olderCertificate = Certificate.Restore(
            Guid.NewGuid(), userId, olderCourse.Id, DateTime.UtcNow.AddDays(-2), DateTime.UtcNow.AddDays(-2), DateTime.UtcNow.AddDays(-2));
        var newerCertificate = Certificate.Restore(
            Guid.NewGuid(), userId, newerCourse.Id, DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddDays(-1));
        await certificates.CreateAsync(olderCertificate);
        await certificates.CreateAsync(newerCertificate);

        var useCase = new ListMyCertificatesUseCase(certificates, courses);

        var output = await useCase.ExecuteAsync(userId);

        Assert.Equal(2, output.Count);
        Assert.Equal(newerCourse.Id, output.First().CourseId);
        Assert.Equal(newerCourse.Title, output.First().CourseTitle);
        Assert.Equal(olderCourse.Id, output.Last().CourseId);
    }

    [Fact]
    public async Task ExecuteAsync_WhenUserHasNoCertificates_ShouldReturnEmpty()
    {
        var certificates = new FakeCertificateRepository();
        var courses = new FakeCourseRepository();
        var useCase = new ListMyCertificatesUseCase(certificates, courses);

        var output = await useCase.ExecuteAsync(Guid.NewGuid());

        Assert.Empty(output);
    }
}
