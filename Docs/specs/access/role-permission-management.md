# Role and permission management (Admin only)

## Why

Roles and their permissions could only be created through the development seed, and any user with `users.manage` could assign roles — including `Admin` — to anyone (privilege escalation). Backoffice staff (secretaria, mídia, etc.) need their own limited screens, and only the main administrator should decide who gets what.

## Behavior

### Who can do it

Only users holding the **`Admin` role** (policy `AdminOnly`). It is deliberately **not** a permission, so it can not be delegated by editing a role's permissions. The Admin can still make someone else an Admin by assigning them the `Admin` role.

### Endpoints (all `AdminOnly`)

| Method | Route | Purpose |
| --- | --- | --- |
| GET | `/api/admin/roles` | All roles (also inactive) with description, `isSystem`, `memberCount` (active users) and `permissionKeys` |
| GET | `/api/admin/permissions` | The permission catalog (`key`, `name`, `description`) |
| POST | `/api/admin/roles` | Create a role (`name`, `description`) |
| PUT | `/api/admin/roles/{roleId}` | Rename / describe / (de)activate a role |
| PUT | `/api/admin/roles/{roleId}/permissions` | **Replace** the permission set: `{ "permissionKeys": ["visitors.read", ...] }` |
| POST/DELETE | `/api/users/{userId}/roles/{roleId}` | Assign / remove a role (was `ManageUsers`, now `AdminOnly`) |

`GET /api/roles` (active roles, `ManageUsers`) is unchanged; the user edit screen still reads it.

### Rules

- The `Admin` role is a system role: it can not be renamed, deactivated or have its permissions changed (`403`). It already passes every policy.
- Role names are unique, case-insensitive on lookup (`409`). Name ≤ 100, description ≤ 500 (`400`).
- Unknown permission keys are rejected with `400` and listed in `details`.
- The last active Admin can not lose the `Admin` role (`409`), so the system can never end up without an administrator.
- **Changes apply immediately.** Assigning/removing a role, changing a role's permissions, or deactivating a role bumps the `TokenVersion` of every affected user and revokes their refresh tokens. Their current access token is rejected (`401`) and they sign in again to receive the new claims.
- Every change is audited: `RoleCreated`, `RoleUpdated`, `RolePermissionsChanged` (with `granted`/`removed`), `UserRoleAssigned`, `UserRoleUnassigned`.
- `roles.manage` stays in the catalog but is not enforced by any policy (role management is Admin-only); the frontend hides it.

### Permission catalog and the production database

The catalog and the `Admin` role used to be created only by the development seed, which is off in production. The migration `EnsureAdminRoleAndPermissionCatalog` inserts them (idempotent) and gives `Admin` every permission. **Whenever a new permission is introduced, add a migration that inserts it and grants it to `Admin`** — do not rely on the seed.

### First administrator in production

The seed is disabled, so promote a user who already registered (and confirmed their e-mail) once, directly in the database:

```sql
INSERT INTO user_roles ("UserId", "RoleId", "CreatedAt")
SELECT u."Id", r."Id", now()
FROM users u, roles r
WHERE u."Email" = 'quem.vai.ser.admin@exemplo.com' AND r."Name" = 'Admin'
ON CONFLICT DO NOTHING;
```

From then on that person manages everything else from the panel.

## Acceptance

- [x] A non-Admin (even with `users.manage` or `roles.manage`) gets `403` on every endpoint above and on assigning/removing roles.
- [x] Admin can create a role, grant/revoke permissions and assign it to a user; the user's next login carries exactly those permissions.
- [x] Revoking a permission invalidates the member's current access token immediately.
- [x] The `Admin` role can not be modified; the last active Admin can not lose it.
- [x] The migration creates the catalog and the Admin role on an empty database and is a no-op on a seeded one (verified on PostgreSQL).
