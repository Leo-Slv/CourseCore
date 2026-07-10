namespace CourseCore.Api.Modules.Media.Application.DTOs;

public class RequestVideoPlaybackInput
{
    public Guid UserId { get; init; }

    public Guid VideoId { get; init; }
}
