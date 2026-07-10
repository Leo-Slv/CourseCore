# Database Seeding

The CourseCore seed is opt-in, idempotent, and does not apply migrations automatically.

Before running the application with seed enabled, apply migrations with the configured PostgreSQL credentials:

```powershell
dotnet ef database update --context CourseCoreDbContext
```

Enable the admin seed with environment variables. Do not store the admin password in committed configuration files.

```powershell
$env:Seed__Admin__Enabled="true"
$env:Seed__Admin__Name="CourseCore Admin"
$env:Seed__Admin__Email="admin@coursecore.local"
$env:Seed__Admin__Password="troque-esta-senha-local"
$env:Seed__Admin__ResetPassword="false"

dotnet run
```

The seed creates or reuses:

- the `Admin` role;
- base permissions for users, roles, areas, courses, videos, progress, and audit logs;
- base areas for admin, courses, media, and progress;
- the admin user;
- the admin user role assignment;
- role permission assignments;
- admin role area access with view and manage permissions.

The admin password is only applied when the user is created. To rotate it intentionally, set:

```powershell
$env:Seed__Admin__ResetPassword="true"
```

The seed does not call `Database.MigrateAsync()`. The database schema must already be up to date.
