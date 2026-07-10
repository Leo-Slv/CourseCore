namespace CourseCore.Api.Modules.Media.Presentation.Requests;

public class RequestVideoPlaybackRequest
{
    public Guid UserId { get; init; }

    public Guid VideoId { get; init; }
}
