# Week 1 RBAC Backend

ASP.NET Core 10 controller-based Web API for a Role-Based Access Control lab. The API uses PostgreSQL, EF Core migrations, and Swagger UI. Week 1 keeps every endpoint anonymous and does not implement login, JWT, or authorization guards.

## Requirements

- .NET 10 SDK
- PostgreSQL running on `localhost:5433` for the included local setup

Create the lab user and database with a PostgreSQL admin account:

```sql
CREATE USER week1_app WITH PASSWORD 'week1_secret';
CREATE DATABASE week1_rbac OWNER week1_app;
```

## Run

From this folder:

```powershell
dotnet tool restore
dotnet restore .\Week1.Rbac.Api\Week1.Rbac.Api.csproj
dotnet tool run dotnet-ef --project .\Week1.Rbac.Api\Week1.Rbac.Api.csproj database update
cd .\Week1.Rbac.Api
dotnet run --urls http://localhost:5080
```

Open Swagger:

```text
http://localhost:5080/swagger
```

Or run the helper script from the repository root:

```bat
run.bat
```

`run.bat` checks PostgreSQL on ports `5433`, `5432`, `5434`, and `5435`, then picks a free API port. If PostgreSQL is not running, run `setup-postgres-db.bat` or start PostgreSQL and create the `week1_rbac` database before running the script again.

## Required Test Flow
1. Create permission `users.read`.
2. Create permission `roles.write`.
3. Create role `Admin` and assign both permissions.
4. Create user `admin@example.com` and assign `Admin` role.
5. Verify `GET /api/users` returns the user with role list.
6. Verify `GET /api/users` returns the user with the assigned role.
7. Verify `GET /api/roles/{id}` returns permission codes for the role.
8. Verify user responses never return `PasswordHash`.

## API Endpoints Overview

### Users (`/api/users`)
- `GET /api/users` → List users with assigned roles
- `GET /api/users/{id}` → Get a user
- `POST /api/users` → Create a user
- `PUT /api/users/{id}` → Update a user
- `DELETE /api/users/{id}` → Delete a user
- `PUT /api/users/{userId}/roles/{roleId}` → Assign a role
- `DELETE /api/users/{userId}/roles/{roleId}` → Remove a role

### Roles (`/api/roles`)
- `GET /api/roles` → List roles with permissions
- `GET /api/roles/{id}` → Get a role
- `POST /api/roles` → Create a role
- `PUT /api/roles/{id}` → Update a role
- `DELETE /api/roles/{id}` → Delete a role
- `PUT /api/roles/{roleId}/permissions/{permissionId}` → Assign a permission
- `DELETE /api/roles/{roleId}/permissions/{permissionId}` → Remove a permission

### Permissions (`/api/permissions`)
- `GET /api/permissions` → List permissions
- `GET /api/permissions/{id}` → Get a permission
- `POST /api/permissions` → Create a permission
- `PUT /api/permissions/{id}` → Update a permission
- `DELETE /api/permissions/{id}` → Delete a permission

### Relationship reads
- `GET /api/user-roles` and `GET /api/user-roles/{userId}/{roleId}`
- `GET /api/role-permissions` and `GET /api/role-permissions/{roleId}/{permissionId}`

## Database Schema

### Users
- `id` (Guid, PK)
- `email` (string, unique)
- `display_name` (string)
- `password_hash` (string)
- `is_active` (bool)
- `created_at` (timestamp)

### Roles
- `id` (Guid, PK)
- `name` (string, unique)
- `description` (string, optional)
- `created_at` (timestamp)

### Permissions
- `id` (Guid, PK)
- `code` (string, unique)
- `description` (string, optional)
- `created_at` (timestamp)

### User Roles (M2M)
- `user_id` (Guid, FK, PK)
- `role_id` (Guid, FK, PK)

### Role Permissions (M2M)
- `role_id` (Guid, FK, PK)
- `permission_id` (Guid, FK, PK)
