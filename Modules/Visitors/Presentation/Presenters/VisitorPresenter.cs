using CourseCore.Api.Modules.Visitors.Application.DTOs;
using CourseCore.Api.Modules.Visitors.Presentation.Requests;
using CourseCore.Api.Modules.Visitors.Presentation.Responses;
using CourseCore.Api.Shared.Application.DTOs;
using CourseCore.Api.Shared.Presentation.Responses;

namespace CourseCore.Api.Modules.Visitors.Presentation.Presenters;

public static class VisitorPresenter
{
    public static RegisterVisitorInput ToInput(RegisterVisitorRequest request) => new()
    {
        Name = request.Name,
        Phone = request.Phone,
        Email = request.Email,
        Address = request.Address,
        CaptchaToken = request.CaptchaToken
    };

    public static ListVisitorsInput ToInput(ListVisitorsRequest request) => new()
    {
        Page = request.Page,
        PageSize = request.PageSize,
        Search = request.Search
    };

    public static RegisterVisitorResponse ToResponse(RegisterVisitorOutput output) => new()
    {
        Id = output.Id,
        SubmittedAt = output.SubmittedAt
    };

    public static VisitorResponse ToResponse(VisitorOutput output) => new()
    {
        Id = output.Id,
        Name = output.Name,
        Phone = output.Phone,
        Email = output.Email,
        Address = output.Address,
        SubmittedAt = output.SubmittedAt
    };

    public static PagedResponse<VisitorResponse> ToResponse(PagedResult<VisitorOutput> output) => new()
    {
        Items = output.Items.Select(ToResponse).ToList(),
        Page = output.Page,
        PageSize = output.PageSize,
        TotalItems = output.TotalItems,
        TotalPages = output.TotalPages
    };
}
