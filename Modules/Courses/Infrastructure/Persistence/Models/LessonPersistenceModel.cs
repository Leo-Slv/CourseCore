using CourseCore.Api.Modules.Media.Infrastructure.Persistence.Models;
using CourseCore.Api.Modules.Progress.Infrastructure.Persistence.Models;

namespace CourseCore.Api.Modules.Courses.Infrastructure.Persistence.Models;

public class LessonPersistenceModel
{
    public Guid Id { get; set; }

    public Guid ModuleId { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public int DisplayOrder { get; set; }

    public bool FreePreview { get; set; }

    public bool Published { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public CourseModulePersistenceModel? Module { get; set; }

    public VideoPersistenceModel? Video { get; set; }

    public ICollection<UserLessonProgressPersistenceModel> UserProgresses { get; set; } = new List<UserLessonProgressPersistenceModel>();
}
