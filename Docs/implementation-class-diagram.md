
```mermaid

classDiagram
    direction LR

    namespace CourseCore.Api.Presentation.Controllers {
        class AuthController {
            +LoginAsync(LoginRequest request) Task~AuthResponse~
            +RefreshAsync(RefreshTokenRequest request) Task~AuthResponse~
            +LogoutAsync() Task
        }

        class UsersController {
            +CreateAsync(CreateUserRequest request) Task~UserResponse~
            +UpdateAsync(Guid id, UpdateUserRequest request) Task~UserResponse~
            +GetByIdAsync(Guid id) Task~UserResponse~
            +ListAsync(UserFilterRequest request) Task~PagedResponse~UserResponse~~
        }

        class AreasController {
            +CreateAsync(CreateAreaRequest request) Task~AreaResponse~
            +UpdateAsync(Guid id, UpdateAreaRequest request) Task~AreaResponse~
            +ListAsync() Task~IReadOnlyList~AreaResponse~~
        }

        class CoursesController {
            +CreateAsync(CreateCourseRequest request) Task~CourseResponse~
            +UpdateAsync(Guid id, UpdateCourseRequest request) Task~CourseResponse~
            +PublishAsync(Guid id) Task~CourseResponse~
            +GetByIdAsync(Guid id) Task~CourseDetailsResponse~
            +ListAvailableAsync() Task~IReadOnlyList~CourseResponse~~
        }

        class VideosController {
            +CreateAsync(CreateVideoRequest request) Task~VideoResponse~
            +RequestPlaybackAsync(Guid videoId) Task~VideoPlaybackResponse~
        }

        class ProgressController {
            +RegisterLessonProgressAsync(RegisterLessonProgressRequest request) Task~LessonProgressResponse~
            +GetCourseProgressAsync(Guid courseId) Task~CourseProgressResponse~
        }
    }

    namespace CourseCore.Api.Presentation.Requests {
        class LoginRequest {
            +string Email
            +string Password
        }

        class CreateUserRequest {
            +string Name
            +string Email
            +string Password
            +List~Guid~ RoleIds
        }

        class CreateAreaRequest {
            +string Name
            +string Slug
            +string Description
            +int DisplayOrder
        }

        class CreateCourseRequest {
            +string Title
            +string Slug
            +string Description
            +List~Guid~ AreaIds
        }

        class CreateVideoRequest {
            +Guid LessonId
            +string Title
            +string StorageProvider
            +string StorageKey
            +int DurationSeconds
        }

        class RegisterLessonProgressRequest {
            +Guid LessonId
            +int WatchedSeconds
            +bool Completed
        }
    }

    namespace CourseCore.Api.Presentation.Presenters {
        class UserPresenter {
            +ToResponse(User user) UserResponse
        }

        class AreaPresenter {
            +ToResponse(Area area) AreaResponse
        }

        class CoursePresenter {
            +ToResponse(Course course) CourseResponse
            +ToDetailsResponse(Course course) CourseDetailsResponse
        }

        class VideoPresenter {
            +ToResponse(Video video) VideoResponse
            +ToPlaybackResponse(Video video, string playbackUrl) VideoPlaybackResponse
        }

        class ProgressPresenter {
            +ToResponse(UserLessonProgress progress) LessonProgressResponse
        }
    }

    namespace CourseCore.Api.Application.UseCases.Auth {
        class LoginUseCase {
            -IUserRepository users
            -IPasswordHasher passwordHasher
            -ITokenService tokenService
            +ExecuteAsync(LoginInput input) Task~AuthOutput~
        }

        class RefreshTokenUseCase {
            -ITokenService tokenService
            +ExecuteAsync(string refreshToken) Task~AuthOutput~
        }
    }

    namespace CourseCore.Api.Application.UseCases.Users {
        class CreateUserUseCase {
            -IUnitOfWork unitOfWork
            -IUserRepository users
            -IPasswordHasher passwordHasher
            +ExecuteAsync(CreateUserInput input) Task~User~
        }

        class UpdateUserUseCase {
            -IUnitOfWork unitOfWork
            -IUserRepository users
            +ExecuteAsync(Guid id, UpdateUserInput input) Task~User~
        }

        class ListUsersUseCase {
            -IUserRepository users
            +ExecuteAsync(UserFilterInput input) Task~PagedResult~User~~
        }
    }

    namespace CourseCore.Api.Application.UseCases.Access {
        class GrantUserAreaAccessUseCase {
            -IUnitOfWork unitOfWork
            -IUserRepository users
            -IAreaRepository areas
            +ExecuteAsync(GrantUserAreaAccessInput input) Task~UserAreaAccess~
        }

        class GrantRoleAreaAccessUseCase {
            -IUnitOfWork unitOfWork
            -IRoleRepository roles
            -IAreaRepository areas
            +ExecuteAsync(GrantRoleAreaAccessInput input) Task~RoleAreaAccess~
        }

        class CheckCourseAccessUseCase {
            -ICourseAccessService accessService
            +ExecuteAsync(Guid userId, Guid courseId) Task~bool~
        }
    }

    namespace CourseCore.Api.Application.UseCases.Courses {
        class CreateCourseUseCase {
            -IUnitOfWork unitOfWork
            -ICourseRepository courses
            -IAreaRepository areas
            +ExecuteAsync(CreateCourseInput input) Task~Course~
        }

        class UpdateCourseUseCase {
            -IUnitOfWork unitOfWork
            -ICourseRepository courses
            +ExecuteAsync(Guid id, UpdateCourseInput input) Task~Course~
        }

        class PublishCourseUseCase {
            -IUnitOfWork unitOfWork
            -ICourseRepository courses
            +ExecuteAsync(Guid id) Task~Course~
        }

        class GetCourseDetailsUseCase {
            -ICourseRepository courses
            -ICourseAccessService accessService
            +ExecuteAsync(Guid userId, Guid courseId) Task~Course~
        }

        class ListAvailableCoursesUseCase {
            -ICourseRepository courses
            -ICourseAccessService accessService
            +ExecuteAsync(Guid userId) Task~IReadOnlyList~Course~~
        }
    }

    namespace CourseCore.Api.Application.UseCases.Media {
        class CreateVideoUseCase {
            -IUnitOfWork unitOfWork
            -IVideoRepository videos
            -ILessonRepository lessons
            +ExecuteAsync(CreateVideoInput input) Task~Video~
        }

        class RequestVideoPlaybackUseCase {
            -IVideoRepository videos
            -ICourseAccessService accessService
            -IVideoStorageService storageService
            +ExecuteAsync(Guid userId, Guid videoId) Task~VideoPlaybackOutput~
        }
    }

    namespace CourseCore.Api.Application.UseCases.Progress {
        class RegisterLessonProgressUseCase {
            -IUnitOfWork unitOfWork
            -IProgressRepository progress
            -ILessonRepository lessons
            +ExecuteAsync(Guid userId, RegisterLessonProgressInput input) Task~UserLessonProgress~
        }

        class GetCourseProgressUseCase {
            -IProgressRepository progress
            +ExecuteAsync(Guid userId, Guid courseId) Task~CourseProgressOutput~
        }
    }

    namespace CourseCore.Api.Application.Services {
        class CourseAccessService {
            -IUserRepository users
            -ICourseRepository courses
            -IAreaRepository areas
            +CanUserAccessCourseAsync(Guid userId, Guid courseId) Task~bool~
            +CanUserAccessAreaAsync(Guid userId, Guid areaId) Task~bool~
        }

        class CurrentUserService {
            +UserId Guid
            +Email string
            +Roles IReadOnlyList~string~
        }
    }

    namespace CourseCore.Api.Application.Contracts {
        class IUnitOfWork {
            <<interface>>
            +ExecuteAsync~T~(Func~Task~T~~ action) Task~T~
            +ExecuteAsync(Func~Task~ action) Task
        }

        class ITokenService {
            <<interface>>
            +GenerateAsync(User user) Task~AuthToken~
            +ValidateRefreshTokenAsync(string token) Task~Guid~
        }

        class IPasswordHasher {
            <<interface>>
            +Hash(string password) string
            +Verify(string password, string hash) bool
        }

        class IVideoStorageService {
            <<interface>>
            +GeneratePlaybackUrlAsync(Video video) Task~string~
            +GetUploadUrlAsync(string storageKey) Task~string~
        }
    }

    namespace CourseCore.Api.Domain.Entities {
        class User {
            +Guid Id
            +string Name
            +Email Email
            +string PasswordHash
            +bool Active
            +DateTime CreatedAt
            +DateTime UpdatedAt
            +ChangeName(string name) void
            +ChangeEmail(Email email) void
            +ChangePasswordHash(string passwordHash) void
            +Activate() void
            +Deactivate() void
        }

        class Role {
            +Guid Id
            +string Name
            +string Description
            +bool Active
            +Activate() void
            +Deactivate() void
        }

        class Permission {
            +Guid Id
            +string Key
            +string Name
            +string Description
        }

        class Area {
            +Guid Id
            +string Name
            +Slug Slug
            +string Description
            +bool Active
            +int DisplayOrder
            +Activate() void
            +Deactivate() void
            +ChangeDisplayOrder(int order) void
        }

        class Course {
            +Guid Id
            +string Title
            +Slug Slug
            +string Description
            +string ThumbnailUrl
            +bool Published
            +int DisplayOrder
            +DateTime? PublishedAt
            +AddModule(CourseModule module) void
            +RemoveModule(Guid moduleId) void
            +AttachArea(Guid areaId) void
            +DetachArea(Guid areaId) void
            +Publish() void
            +Unpublish() void
        }

        class CourseModule {
            +Guid Id
            +Guid CourseId
            +string Title
            +string Description
            +int DisplayOrder
            +bool Published
            +AddLesson(Lesson lesson) void
            +Publish() void
            +Unpublish() void
        }

        class Lesson {
            +Guid Id
            +Guid ModuleId
            +string Title
            +string Description
            +int DisplayOrder
            +bool FreePreview
            +bool Published
            +MarkAsFreePreview() void
            +RemoveFreePreview() void
            +Publish() void
            +Unpublish() void
        }

        class Video {
            +Guid Id
            +Guid LessonId
            +string Title
            +string Description
            +VideoStorageProvider StorageProvider
            +string StorageKey
            +string PlaybackUrl
            +string ThumbnailUrl
            +int DurationSeconds
            +long SizeBytes
            +VideoStatus Status
            +MarkAsProcessing() void
            +MarkAsReady(string playbackUrl) void
            +MarkAsFailed() void
        }

        class UserCourseProgress {
            +Guid Id
            +Guid UserId
            +Guid CourseId
            +decimal ProgressPercent
            +DateTime StartedAt
            +DateTime? CompletedAt
            +Recalculate(decimal progressPercent) void
            +MarkAsCompleted() void
        }

        class UserLessonProgress {
            +Guid Id
            +Guid UserId
            +Guid LessonId
            +bool Completed
            +int WatchedSeconds
            +DateTime LastWatchedAt
            +DateTime? CompletedAt
            +RegisterWatch(int watchedSeconds) void
            +MarkAsCompleted() void
        }

        class UserAreaAccess {
            +Guid Id
            +Guid UserId
            +Guid AreaId
            +bool CanView
            +bool CanManage
            +DateTime? StartsAt
            +DateTime? ExpiresAt
            +IsValidAt(DateTime date) bool
            +Revoke() void
        }

        class RoleAreaAccess {
            +Guid Id
            +Guid RoleId
            +Guid AreaId
            +bool CanView
            +bool CanManage
        }

        class AuditLog {
            +Guid Id
            +Guid? UserId
            +string Action
            +string EntityName
            +Guid? EntityId
            +string MetadataJson
            +DateTime CreatedAt
        }
    }

    namespace CourseCore.Api.Domain.ValueObjects {
        class Email {
            +string Value
            +Create(string value) Email
        }

        class Slug {
            +string Value
            +Create(string value) Slug
        }
    }

    namespace CourseCore.Api.Domain.Enums {
        class VideoStatus {
            <<enumeration>>
            Processing
            Ready
            Failed
        }

        class VideoStorageProvider {
            <<enumeration>>
            Local
            S3
            AzureBlob
            CloudflareR2
            Vimeo
            Mux
        }
    }

    namespace CourseCore.Api.Domain.Repositories {
        class IUserRepository {
            <<interface>>
            +FindByIdAsync(Guid id) Task~User?~
            +FindByEmailAsync(Email email) Task~User?~
            +CreateAsync(User user) Task
            +UpdateAsync(User user) Task
        }

        class IRoleRepository {
            <<interface>>
            +FindByIdAsync(Guid id) Task~Role?~
            +FindByUserIdAsync(Guid userId) Task~IReadOnlyList~Role~~
        }

        class IAreaRepository {
            <<interface>>
            +FindByIdAsync(Guid id) Task~Area?~
            +FindBySlugAsync(Slug slug) Task~Area?~
            +ListByUserAccessAsync(Guid userId) Task~IReadOnlyList~Area~~
        }

        class ICourseRepository {
            <<interface>>
            +FindByIdAsync(Guid id) Task~Course?~
            +FindDetailsByIdAsync(Guid id) Task~Course?~
            +ListByAreaIdsAsync(IReadOnlyList~Guid~ areaIds) Task~IReadOnlyList~Course~~
            +CreateAsync(Course course) Task
            +UpdateAsync(Course course) Task
        }

        class ILessonRepository {
            <<interface>>
            +FindByIdAsync(Guid id) Task~Lesson?~
        }

        class IVideoRepository {
            <<interface>>
            +FindByIdAsync(Guid id) Task~Video?~
            +FindByLessonIdAsync(Guid lessonId) Task~Video?~
            +CreateAsync(Video video) Task
            +UpdateAsync(Video video) Task
        }

        class IProgressRepository {
            <<interface>>
            +FindLessonProgressAsync(Guid userId, Guid lessonId) Task~UserLessonProgress?~
            +FindCourseProgressAsync(Guid userId, Guid courseId) Task~UserCourseProgress?~
            +SaveLessonProgressAsync(UserLessonProgress progress) Task
            +SaveCourseProgressAsync(UserCourseProgress progress) Task
        }
    }

    namespace CourseCore.Api.Infrastructure.Persistence.Models {
        class UserPersistenceModel {
            +Guid Id
            +string Name
            +string Email
            +string PasswordHash
            +bool Active
            +DateTime? EmailVerifiedAt
            +DateTime CreatedAt
            +DateTime UpdatedAt
            +ICollection~UserRolePersistenceModel~ UserRoles
            +ICollection~UserAreaAccessPersistenceModel~ AreaAccesses
        }

        class RolePersistenceModel {
            +Guid Id
            +string Name
            +string Description
            +bool Active
            +DateTime CreatedAt
            +DateTime UpdatedAt
            +ICollection~UserRolePersistenceModel~ UserRoles
            +ICollection~RolePermissionPersistenceModel~ RolePermissions
            +ICollection~RoleAreaAccessPersistenceModel~ AreaAccesses
        }

        class PermissionPersistenceModel {
            +Guid Id
            +string Key
            +string Name
            +string Description
            +DateTime CreatedAt
            +DateTime UpdatedAt
            +ICollection~RolePermissionPersistenceModel~ RolePermissions
        }

        class UserRolePersistenceModel {
            +Guid Id
            +Guid UserId
            +Guid RoleId
            +DateTime CreatedAt
            +UserPersistenceModel User
            +RolePersistenceModel Role
        }

        class RolePermissionPersistenceModel {
            +Guid Id
            +Guid RoleId
            +Guid PermissionId
            +DateTime CreatedAt
            +RolePersistenceModel Role
            +PermissionPersistenceModel Permission
        }

        class AreaPersistenceModel {
            +Guid Id
            +string Name
            +string Slug
            +string Description
            +bool Active
            +int DisplayOrder
            +DateTime CreatedAt
            +DateTime UpdatedAt
            +ICollection~CourseAreaPersistenceModel~ CourseAreas
            +ICollection~UserAreaAccessPersistenceModel~ UserAccesses
            +ICollection~RoleAreaAccessPersistenceModel~ RoleAccesses
        }

        class CoursePersistenceModel {
            +Guid Id
            +string Title
            +string Slug
            +string Description
            +string ThumbnailUrl
            +bool Published
            +int DisplayOrder
            +DateTime? PublishedAt
            +DateTime CreatedAt
            +DateTime UpdatedAt
            +ICollection~CourseAreaPersistenceModel~ CourseAreas
            +ICollection~CourseModulePersistenceModel~ Modules
        }

        class CourseAreaPersistenceModel {
            +Guid Id
            +Guid CourseId
            +Guid AreaId
            +DateTime CreatedAt
            +CoursePersistenceModel Course
            +AreaPersistenceModel Area
        }

        class CourseModulePersistenceModel {
            +Guid Id
            +Guid CourseId
            +string Title
            +string Description
            +int DisplayOrder
            +bool Published
            +DateTime CreatedAt
            +DateTime UpdatedAt
            +CoursePersistenceModel Course
            +ICollection~LessonPersistenceModel~ Lessons
        }

        class LessonPersistenceModel {
            +Guid Id
            +Guid ModuleId
            +string Title
            +string Description
            +int DisplayOrder
            +bool FreePreview
            +bool Published
            +DateTime CreatedAt
            +DateTime UpdatedAt
            +CourseModulePersistenceModel Module
            +VideoPersistenceModel Video
        }

        class VideoPersistenceModel {
            +Guid Id
            +Guid LessonId
            +string Title
            +string Description
            +string StorageProvider
            +string StorageKey
            +string PlaybackUrl
            +string ThumbnailUrl
            +int DurationSeconds
            +long SizeBytes
            +string Status
            +DateTime CreatedAt
            +DateTime UpdatedAt
            +LessonPersistenceModel Lesson
        }

        class UserAreaAccessPersistenceModel {
            +Guid Id
            +Guid UserId
            +Guid AreaId
            +bool CanView
            +bool CanManage
            +DateTime? StartsAt
            +DateTime? ExpiresAt
            +DateTime CreatedAt
            +UserPersistenceModel User
            +AreaPersistenceModel Area
        }

        class RoleAreaAccessPersistenceModel {
            +Guid Id
            +Guid RoleId
            +Guid AreaId
            +bool CanView
            +bool CanManage
            +DateTime CreatedAt
            +RolePersistenceModel Role
            +AreaPersistenceModel Area
        }

        class UserCourseProgressPersistenceModel {
            +Guid Id
            +Guid UserId
            +Guid CourseId
            +decimal ProgressPercent
            +DateTime StartedAt
            +DateTime? CompletedAt
            +DateTime UpdatedAt
            +UserPersistenceModel User
            +CoursePersistenceModel Course
        }

        class UserLessonProgressPersistenceModel {
            +Guid Id
            +Guid UserId
            +Guid LessonId
            +bool Completed
            +int WatchedSeconds
            +DateTime LastWatchedAt
            +DateTime? CompletedAt
            +DateTime CreatedAt
            +DateTime UpdatedAt
            +UserPersistenceModel User
            +LessonPersistenceModel Lesson
        }

        class AuditLogPersistenceModel {
            +Guid Id
            +Guid? UserId
            +string Action
            +string EntityName
            +Guid? EntityId
            +string MetadataJson
            +DateTime CreatedAt
        }
    }

    namespace CourseCore.Api.Infrastructure.Persistence.Mappers {
        class UserMapper {
            +ToDomain(UserPersistenceModel model) User
            +ToPersistence(User domain) UserPersistenceModel
            +ApplyChanges(User domain, UserPersistenceModel model) void
        }

        class RoleMapper {
            +ToDomain(RolePersistenceModel model) Role
            +ToPersistence(Role domain) RolePersistenceModel
        }

        class PermissionMapper {
            +ToDomain(PermissionPersistenceModel model) Permission
            +ToPersistence(Permission domain) PermissionPersistenceModel
        }

        class AreaMapper {
            +ToDomain(AreaPersistenceModel model) Area
            +ToPersistence(Area domain) AreaPersistenceModel
            +ApplyChanges(Area domain, AreaPersistenceModel model) void
        }

        class CourseMapper {
            +ToDomain(CoursePersistenceModel model) Course
            +ToPersistence(Course domain) CoursePersistenceModel
            +ApplyChanges(Course domain, CoursePersistenceModel model) void
        }

        class CourseModuleMapper {
            +ToDomain(CourseModulePersistenceModel model) CourseModule
            +ToPersistence(CourseModule domain) CourseModulePersistenceModel
        }

        class LessonMapper {
            +ToDomain(LessonPersistenceModel model) Lesson
            +ToPersistence(Lesson domain) LessonPersistenceModel
        }

        class VideoMapper {
            +ToDomain(VideoPersistenceModel model) Video
            +ToPersistence(Video domain) VideoPersistenceModel
            +ApplyChanges(Video domain, VideoPersistenceModel model) void
        }

        class UserAreaAccessMapper {
            +ToDomain(UserAreaAccessPersistenceModel model) UserAreaAccess
            +ToPersistence(UserAreaAccess domain) UserAreaAccessPersistenceModel
        }

        class RoleAreaAccessMapper {
            +ToDomain(RoleAreaAccessPersistenceModel model) RoleAreaAccess
            +ToPersistence(RoleAreaAccess domain) RoleAreaAccessPersistenceModel
        }

        class ProgressMapper {
            +ToDomain(UserLessonProgressPersistenceModel model) UserLessonProgress
            +ToPersistence(UserLessonProgress domain) UserLessonProgressPersistenceModel
            +ToDomain(UserCourseProgressPersistenceModel model) UserCourseProgress
            +ToPersistence(UserCourseProgress domain) UserCourseProgressPersistenceModel
        }

        class AuditLogMapper {
            +ToDomain(AuditLogPersistenceModel model) AuditLog
            +ToPersistence(AuditLog domain) AuditLogPersistenceModel
        }
    }

    namespace CourseCore.Api.Infrastructure.Persistence {
        class CourseCoreDbContext {
            +DbSet~UserPersistenceModel~ Users
            +DbSet~RolePersistenceModel~ Roles
            +DbSet~PermissionPersistenceModel~ Permissions
            +DbSet~UserRolePersistenceModel~ UserRoles
            +DbSet~RolePermissionPersistenceModel~ RolePermissions
            +DbSet~AreaPersistenceModel~ Areas
            +DbSet~CoursePersistenceModel~ Courses
            +DbSet~CourseAreaPersistenceModel~ CourseAreas
            +DbSet~CourseModulePersistenceModel~ CourseModules
            +DbSet~LessonPersistenceModel~ Lessons
            +DbSet~VideoPersistenceModel~ Videos
            +DbSet~UserAreaAccessPersistenceModel~ UserAreaAccesses
            +DbSet~RoleAreaAccessPersistenceModel~ RoleAreaAccesses
            +DbSet~UserLessonProgressPersistenceModel~ UserLessonProgress
            +DbSet~UserCourseProgressPersistenceModel~ UserCourseProgress
            +DbSet~AuditLogPersistenceModel~ AuditLogs
            +SaveChangesAsync() Task~int~
        }

        class EfUnitOfWork {
            -CourseCoreDbContext dbContext
            +ExecuteAsync~T~(Func~Task~T~~ action) Task~T~
            +ExecuteAsync(Func~Task~ action) Task
        }

        class EfUserRepository {
            -CourseCoreDbContext dbContext
            -UserMapper mapper
        }

        class EfRoleRepository {
            -CourseCoreDbContext dbContext
            -RoleMapper mapper
        }

        class EfAreaRepository {
            -CourseCoreDbContext dbContext
            -AreaMapper mapper
            -UserAreaAccessMapper userAreaAccessMapper
            -RoleAreaAccessMapper roleAreaAccessMapper
        }

        class EfCourseRepository {
            -CourseCoreDbContext dbContext
            -CourseMapper mapper
        }

        class EfLessonRepository {
            -CourseCoreDbContext dbContext
            -LessonMapper mapper
        }

        class EfVideoRepository {
            -CourseCoreDbContext dbContext
            -VideoMapper mapper
        }

        class EfProgressRepository {
            -CourseCoreDbContext dbContext
            -ProgressMapper mapper
        }
    }

    namespace CourseCore.Api.Infrastructure.Security {
        class JwtTokenService {
            +GenerateAsync(User user) Task~AuthToken~
            +ValidateRefreshTokenAsync(string token) Task~Guid~
        }

        class BCryptPasswordHasher {
            +Hash(string password) string
            +Verify(string password, string hash) bool
        }
    }

    namespace CourseCore.Api.Infrastructure.Storage {
        class VideoStorageService {
            +GeneratePlaybackUrlAsync(Video video) Task~string~
            +GetUploadUrlAsync(string storageKey) Task~string~
        }
    }

    AuthController --> LoginUseCase
    AuthController --> RefreshTokenUseCase
    UsersController --> CreateUserUseCase
    UsersController --> UpdateUserUseCase
    UsersController --> ListUsersUseCase
    AreasController --> GrantUserAreaAccessUseCase
    AreasController --> GrantRoleAreaAccessUseCase
    CoursesController --> CreateCourseUseCase
    CoursesController --> PublishCourseUseCase
    CoursesController --> GetCourseDetailsUseCase
    CoursesController --> ListAvailableCoursesUseCase
    VideosController --> CreateVideoUseCase
    VideosController --> RequestVideoPlaybackUseCase
    ProgressController --> RegisterLessonProgressUseCase
    ProgressController --> GetCourseProgressUseCase

    CreateUserUseCase --> IUnitOfWork
    CreateUserUseCase --> IUserRepository
    CreateUserUseCase --> IPasswordHasher
    LoginUseCase --> IUserRepository
    LoginUseCase --> IPasswordHasher
    LoginUseCase --> ITokenService

    CreateCourseUseCase --> IUnitOfWork
    CreateCourseUseCase --> ICourseRepository
    CreateCourseUseCase --> IAreaRepository
    GetCourseDetailsUseCase --> ICourseRepository
    GetCourseDetailsUseCase --> CourseAccessService
    RequestVideoPlaybackUseCase --> IVideoRepository
    RequestVideoPlaybackUseCase --> CourseAccessService
    RequestVideoPlaybackUseCase --> IVideoStorageService

    CourseAccessService --> IUserRepository
    CourseAccessService --> ICourseRepository
    CourseAccessService --> IAreaRepository

    User "1" --> "0..*" UserAreaAccess
    User "1" --> "0..*" UserLessonProgress
    User "1" --> "0..*" UserCourseProgress
    Role "1" --> "0..*" RoleAreaAccess
    Area "1" --> "0..*" UserAreaAccess
    Area "1" --> "0..*" RoleAreaAccess
    Course "1" --> "0..*" CourseModule
    CourseModule "1" --> "0..*" Lesson
    Lesson "1" --> "0..1" Video
    Lesson "1" --> "0..*" UserLessonProgress
    Course "1" --> "0..*" UserCourseProgress

    UserPersistenceModel "1" --> "0..*" UserRolePersistenceModel
    RolePersistenceModel "1" --> "0..*" UserRolePersistenceModel
    RolePersistenceModel "1" --> "0..*" RolePermissionPersistenceModel
    PermissionPersistenceModel "1" --> "0..*" RolePermissionPersistenceModel

    UserPersistenceModel "1" --> "0..*" UserAreaAccessPersistenceModel
    AreaPersistenceModel "1" --> "0..*" UserAreaAccessPersistenceModel
    RolePersistenceModel "1" --> "0..*" RoleAreaAccessPersistenceModel
    AreaPersistenceModel "1" --> "0..*" RoleAreaAccessPersistenceModel

    CoursePersistenceModel "1" --> "0..*" CourseAreaPersistenceModel
    AreaPersistenceModel "1" --> "0..*" CourseAreaPersistenceModel
    CoursePersistenceModel "1" --> "0..*" CourseModulePersistenceModel
    CourseModulePersistenceModel "1" --> "0..*" LessonPersistenceModel
    LessonPersistenceModel "1" --> "0..1" VideoPersistenceModel

    UserPersistenceModel "1" --> "0..*" UserCourseProgressPersistenceModel
    CoursePersistenceModel "1" --> "0..*" UserCourseProgressPersistenceModel
    UserPersistenceModel "1" --> "0..*" UserLessonProgressPersistenceModel
    LessonPersistenceModel "1" --> "0..*" UserLessonProgressPersistenceModel

    IUserRepository <|.. EfUserRepository
    IRoleRepository <|.. EfRoleRepository
    IAreaRepository <|.. EfAreaRepository
    ICourseRepository <|.. EfCourseRepository
    ILessonRepository <|.. EfLessonRepository
    IVideoRepository <|.. EfVideoRepository
    IProgressRepository <|.. EfProgressRepository
    IUnitOfWork <|.. EfUnitOfWork
    ITokenService <|.. JwtTokenService
    IPasswordHasher <|.. BCryptPasswordHasher
    IVideoStorageService <|.. VideoStorageService

    EfUserRepository --> CourseCoreDbContext
    EfRoleRepository --> CourseCoreDbContext
    EfAreaRepository --> CourseCoreDbContext
    EfCourseRepository --> CourseCoreDbContext
    EfLessonRepository --> CourseCoreDbContext
    EfVideoRepository --> CourseCoreDbContext
    EfProgressRepository --> CourseCoreDbContext
    EfUnitOfWork --> CourseCoreDbContext

    EfUserRepository --> UserMapper
    EfRoleRepository --> RoleMapper
    EfAreaRepository --> AreaMapper
    EfAreaRepository --> UserAreaAccessMapper
    EfAreaRepository --> RoleAreaAccessMapper
    EfCourseRepository --> CourseMapper
    EfLessonRepository --> LessonMapper
    EfVideoRepository --> VideoMapper
    EfProgressRepository --> ProgressMapper

    UserMapper --> UserPersistenceModel
    UserMapper --> User
    RoleMapper --> RolePersistenceModel
    RoleMapper --> Role
    AreaMapper --> AreaPersistenceModel
    AreaMapper --> Area
    CourseMapper --> CoursePersistenceModel
    CourseMapper --> Course
    CourseModuleMapper --> CourseModulePersistenceModel
    CourseModuleMapper --> CourseModule
    LessonMapper --> LessonPersistenceModel
    LessonMapper --> Lesson
    VideoMapper --> VideoPersistenceModel
    VideoMapper --> Video
    ProgressMapper --> UserLessonProgressPersistenceModel
    ProgressMapper --> UserCourseProgressPersistenceModel
    ProgressMapper --> UserLessonProgress
    ProgressMapper --> UserCourseProgress

    CourseCoreDbContext --> UserPersistenceModel
    CourseCoreDbContext --> RolePersistenceModel
    CourseCoreDbContext --> PermissionPersistenceModel
    CourseCoreDbContext --> AreaPersistenceModel
    CourseCoreDbContext --> CoursePersistenceModel
    CourseCoreDbContext --> CourseModulePersistenceModel
    CourseCoreDbContext --> LessonPersistenceModel
    CourseCoreDbContext --> VideoPersistenceModel
    CourseCoreDbContext --> UserAreaAccessPersistenceModel
    CourseCoreDbContext --> RoleAreaAccessPersistenceModel
    CourseCoreDbContext --> UserLessonProgressPersistenceModel
    CourseCoreDbContext --> UserCourseProgressPersistenceModel
    CourseCoreDbContext --> AuditLogPersistenceModel

    ```