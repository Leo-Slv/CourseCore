using CourseCore.Api.Modules.Visitors.Domain.Entities;
using CourseCore.Api.Shared.Domain.Exceptions;
using CourseCore.Api.Shared.Domain.ValueObjects;

namespace CourseCore.Api.Tests.Domain.Visitors;

public class VisitorTests
{
    [Fact]
    public void Create_WhenDataIsValid_ShouldTrimAndKeepValues()
    {
        var visitor = Visitor.Create(
            "  Ana Lima  ",
            "11987654321",
            Email.Create("Ana@Example.com"),
            "  Rua A, 10, Centro, São Paulo  ");

        Assert.Equal("Ana Lima", visitor.Name);
        Assert.Equal("11987654321", visitor.Phone);
        Assert.Equal("ana@example.com", visitor.Email.Value);
        Assert.Equal("Rua A, 10, Centro, São Paulo", visitor.Address);
    }

    [Theory]
    [InlineData("(11) 98765-4321", "11987654321")]
    [InlineData("(11) 3456-7890", "1134567890")]
    [InlineData("11 98765 4321", "11987654321")]
    public void Create_WhenPhoneIsMasked_ShouldStoreDigitsOnly(string phone, string expected)
    {
        var visitor = Visitor.Create("Ana", phone, Email.Create("ana@example.com"), null);

        Assert.Equal(expected, visitor.Phone);
    }

    [Theory]
    [InlineData("")]
    [InlineData("(11) 8765-432")]
    [InlineData("+55 (11) 98765-4321")]
    [InlineData("abc")]
    public void Create_WhenPhoneDoesNotHaveTenOrElevenDigits_ShouldThrow(string phone)
    {
        Assert.Throws<DomainException>(() => Visitor.Create("Ana", phone, Email.Create("ana@example.com"), null));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(" A ")]
    public void Create_WhenNameHasFewerThanTwoCharacters_ShouldThrow(string name)
    {
        Assert.Throws<DomainException>(() => Visitor.Create(name, "11987654321", Email.Create("ana@example.com"), null));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WhenAddressIsBlank_ShouldStoreNull(string? address)
    {
        var visitor = Visitor.Create("Ana", "11987654321", Email.Create("ana@example.com"), address);

        Assert.Null(visitor.Address);
    }

    [Fact]
    public void NewVisitor_ShouldNotBeContacted()
    {
        var visitor = Visitor.Create("Ana", "11987654321", Email.Create("ana@example.com"), null);

        Assert.False(visitor.IsContacted);
        Assert.Null(visitor.ContactedAt);
        Assert.Null(visitor.ContactedByUserId);
    }

    [Fact]
    public void MarkContacted_ShouldRecordWhenAndByWhomAndBeIdempotent()
    {
        var visitor = Visitor.Create("Ana", "11987654321", Email.Create("ana@example.com"), null);
        var userId = Guid.NewGuid();

        var first = visitor.MarkContacted(userId);
        var contactedAt = visitor.ContactedAt;
        var second = visitor.MarkContacted(Guid.NewGuid());

        Assert.True(first);
        Assert.False(second);
        Assert.True(visitor.IsContacted);
        Assert.Equal(contactedAt, visitor.ContactedAt);
        Assert.Equal(userId, visitor.ContactedByUserId);
    }

    [Fact]
    public void MarkContacted_WhenUserIdIsEmpty_ShouldThrow()
    {
        var visitor = Visitor.Create("Ana", "11987654321", Email.Create("ana@example.com"), null);

        Assert.Throws<DomainException>(() => visitor.MarkContacted(Guid.Empty));
    }

    [Fact]
    public void ClearContacted_ShouldResetStateAndReportChange()
    {
        var visitor = Visitor.Create("Ana", "11987654321", Email.Create("ana@example.com"), null);
        visitor.MarkContacted(Guid.NewGuid());

        var cleared = visitor.ClearContacted();
        var clearedAgain = visitor.ClearContacted();

        Assert.True(cleared);
        Assert.False(clearedAgain);
        Assert.False(visitor.IsContacted);
        Assert.Null(visitor.ContactedByUserId);
    }
}
