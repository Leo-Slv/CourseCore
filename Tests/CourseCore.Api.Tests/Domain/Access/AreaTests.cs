using CourseCore.Api.Modules.Access.Domain.Entities;
using CourseCore.Api.Shared.Domain.Exceptions;
using CourseCore.Api.Shared.Domain.ValueObjects;

namespace CourseCore.Api.Tests.Domain.Access;

public class AreaTests
{
    [Fact]
    public void Create_WhenDataIsValid_ShouldCreateActiveArea()
    {
        var area = CreateArea();

        Assert.True(area.Active);
        Assert.Equal("Area", area.Name);
        Assert.Equal("area", area.Slug.Value);
        Assert.Equal(0, area.DisplayOrder);
    }

    [Fact]
    public void Create_WhenNameIsEmpty_ShouldThrow()
    {
        Assert.Throws<DomainException>(() => Area.Create(string.Empty, Slug.Create("area"), "Description", 0));
    }

    [Fact]
    public void Create_WhenDisplayOrderIsNegative_ShouldThrow()
    {
        Assert.Throws<DomainException>(() => Area.Create("Area", Slug.Create("area"), "Description", -1));
    }

    [Fact]
    public void ChangeName_WhenNameIsValid_ShouldUpdateName()
    {
        var area = CreateArea();

        area.ChangeName("Updated");

        Assert.Equal("Updated", area.Name);
    }

    [Fact]
    public void ChangeName_WhenNameIsEmpty_ShouldThrow()
    {
        var area = CreateArea();

        Assert.Throws<DomainException>(() => area.ChangeName(string.Empty));
    }

    [Fact]
    public void ChangeSlug_WhenSlugIsValid_ShouldUpdateSlug()
    {
        var area = CreateArea();
        var newSlug = Slug.Create("updated-area");

        area.ChangeSlug(newSlug);

        Assert.Equal(newSlug, area.Slug);
    }

    [Fact]
    public void ChangeDescription_WhenDescriptionIsProvided_ShouldNormalizeAndUpdate()
    {
        var area = CreateArea();

        area.ChangeDescription("  Updated description  ");

        Assert.Equal("Updated description", area.Description);
    }

    [Fact]
    public void ChangeDisplayOrder_WhenNegative_ShouldThrow()
    {
        var area = CreateArea();

        Assert.Throws<DomainException>(() => area.ChangeDisplayOrder(-1));
    }

    [Fact]
    public void Activate_WhenAreaIsInactive_ShouldActivate()
    {
        var area = CreateArea();
        area.Deactivate();

        area.Activate();

        Assert.True(area.Active);
    }

    [Fact]
    public void Deactivate_WhenAreaIsActive_ShouldDeactivate()
    {
        var area = CreateArea();

        area.Deactivate();

        Assert.False(area.Active);
    }

    private static Area CreateArea()
    {
        return Area.Create("Area", Slug.Create("area"), "Description", 0);
    }
}
