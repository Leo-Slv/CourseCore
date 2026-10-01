using CourseCore.Api.Modules.AuditLogs.Application.Constants;
using CourseCore.Api.Modules.Visitors.Application.DTOs;
using CourseCore.Api.Modules.Visitors.Application.UseCases;
using CourseCore.Api.Modules.Visitors.Application.Validation;
using CourseCore.Api.Shared.Application.Exceptions;
using CourseCore.Api.Shared.Domain.Exceptions;
using CourseCore.Api.Tests.TestDoubles;

namespace CourseCore.Api.Tests.Application.Visitors;

public class RegisterVisitorUseCaseTests
{
    private readonly FakeCaptchaVerificationService _captcha = new();
    private readonly FakeVisitorRepository _visitors = new();
    private readonly FakeAuditLogService _auditLogs = new();

    [Fact]
    public async Task ExecuteAsync_WhenDataIsValid_ShouldPersistVisitorAndReturnIdOnly()
    {
        var output = await CreateUseCase().ExecuteAsync(ValidInput());

        var visitor = Assert.Single(_visitors.Visitors);
        Assert.Equal(visitor.Id, output.Id);
        Assert.Equal(visitor.CreatedAt, output.SubmittedAt);
        Assert.Equal("Ana Lima", visitor.Name);
        Assert.Equal("11987654321", visitor.Phone);
        Assert.Equal("ana@example.com", visitor.Email.Value);
    }

    [Fact]
    public async Task ExecuteAsync_WhenDataIsValid_ShouldAuditWithoutPersonalData()
    {
        var output = await CreateUseCase().ExecuteAsync(ValidInput());

        var auditLog = Assert.Single(_auditLogs.Entries);
        Assert.Equal(AuditLogActionNames.VisitorRegistered, auditLog.Action);
        Assert.Equal("Visitor", auditLog.EntityName);
        Assert.Equal(output.Id, auditLog.EntityId);
        Assert.Null(auditLog.UserId);
        Assert.Empty(auditLog.Metadata);
    }

    [Fact]
    public async Task ExecuteAsync_WhenCaptchaIsInvalid_ShouldThrowAndPersistNothing()
    {
        _captcha.Result = false;

        await Assert.ThrowsAsync<ApplicationValidationException>(() => CreateUseCase().ExecuteAsync(ValidInput()));

        Assert.Empty(_visitors.Visitors);
        Assert.Empty(_auditLogs.Entries);
    }

    [Fact]
    public async Task ExecuteAsync_WhenSameDataIsSubmittedTwice_ShouldCreateTwoRecords()
    {
        var useCase = CreateUseCase();

        var first = await useCase.ExecuteAsync(ValidInput());
        var second = await useCase.ExecuteAsync(ValidInput());

        Assert.NotEqual(first.Id, second.Id);
        Assert.Equal(2, _visitors.Visitors.Count);
    }

    [Fact]
    public async Task ExecuteAsync_WhenAddressIsOmitted_ShouldPersistNullAddress()
    {
        await CreateUseCase().ExecuteAsync(ValidInput(address: null));

        Assert.Null(Assert.Single(_visitors.Visitors).Address);
    }

    [Theory]
    [InlineData("", "11987654321", "ana@example.com")]
    [InlineData("Ana", "", "ana@example.com")]
    [InlineData("Ana", "11987654321", "")]
    public async Task ExecuteAsync_WhenRequiredFieldIsMissing_ShouldThrowValidation(string name, string phone, string email)
    {
        await Assert.ThrowsAsync<ApplicationValidationException>(() => CreateUseCase().ExecuteAsync(new RegisterVisitorInput
        {
            Name = name,
            Phone = phone,
            Email = email,
            CaptchaToken = "token"
        }));

        Assert.Empty(_visitors.Visitors);
    }

    [Fact]
    public async Task ExecuteAsync_WhenEmailIsMalformed_ShouldThrowDomainException()
    {
        await Assert.ThrowsAsync<DomainException>(() => CreateUseCase().ExecuteAsync(ValidInput(email: "not-an-email")));

        Assert.Empty(_visitors.Visitors);
    }

    [Fact]
    public async Task ExecuteAsync_WhenAddressExceedsLimit_ShouldThrowValidation()
    {
        var address = new string('a', VisitorValidationLimits.AddressMaxLength + 1);

        await Assert.ThrowsAsync<ApplicationValidationException>(() => CreateUseCase().ExecuteAsync(ValidInput(address: address)));
    }

    [Fact]
    public async Task ExecuteAsync_WhenNameExceedsLimit_ShouldThrowValidation()
    {
        var name = new string('a', VisitorValidationLimits.NameMaxLength + 1);

        await Assert.ThrowsAsync<ApplicationValidationException>(() => CreateUseCase().ExecuteAsync(ValidInput(name: name)));
    }

    private RegisterVisitorUseCase CreateUseCase()
    {
        return new RegisterVisitorUseCase(_captcha, _visitors, new FakeUnitOfWork(), _auditLogs);
    }

    private static RegisterVisitorInput ValidInput(
        string name = "Ana Lima",
        string email = "Ana@Example.com",
        string? address = "Rua A, 10, Centro, São Paulo")
    {
        return new RegisterVisitorInput
        {
            Name = name,
            Phone = "(11) 98765-4321",
            Email = email,
            Address = address,
            CaptchaToken = "token"
        };
    }
}
