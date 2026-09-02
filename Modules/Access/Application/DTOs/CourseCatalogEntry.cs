using CourseCore.Api.Modules.Courses.Domain.Entities;

namespace CourseCore.Api.Modules.Access.Application.DTOs;

public sealed record CourseCatalogEntry(Course Course, bool HasAccess);
