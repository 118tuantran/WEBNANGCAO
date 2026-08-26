# Bai Thuyet Trinh Week 1 - RBAC

## 1. Gioi thieu

Em xin trinh bay bai Week 1 ve xay dung he thong RBAC, viet tat cua Role-Based Access Control.

RBAC la mo hinh phan quyen dua tren vai tro:

- User duoc gan Role.
- Role duoc gan Permission.
- Permission mo ta quyen thuc hien mot hanh dong, vi du `users.read` hoac `roles.write`.

Muc tieu cua bai la xay dung API quan ly User, Role, Permission va hai quan he User-Role, Role-Permission.

## 2. Cong nghe su dung

- ASP.NET Core Web API .NET 10
- Entity Framework Core
- PostgreSQL
- Swagger/OpenAPI
- EF Core Migration

Ung dung chay bang `dotnet run` va khong yeu cau Docker.

## 3. Thiet ke database

He thong co 5 bang RBAC:

1. `users`
2. `roles`
3. `permissions`
4. `user_roles`
5. `role_permissions`

`user_roles` va `role_permissions` la cac bang trung gian cho quan he many-to-many.

### Khoa chinh

- `users.Id`
- `roles.Id`
- `permissions.Id`
- `(UserId, RoleId)` trong `user_roles`
- `(RoleId, PermissionId)` trong `role_permissions`

### Unique key

- `users.Email`
- `roles.Name`
- `permissions.Code`

## 4. Luong hoat dong chinh

Vi du luong tao du lieu:

1. Tao permission `users.read`.
2. Tao role `Admin`.
3. Gan permission `users.read` cho role `Admin`.
4. Tao user `admin@example.com`.
5. Gan role `Admin` cho user.
6. Truy van user se thay role duoc gan.
7. Truy van role se thay cac permission cua role.

### Endpoint gan role cho user

```text
PUT    /api/users/{userId}/roles/{roleId}
DELETE /api/users/{userId}/roles/{roleId}
```

### Endpoint gan permission cho role

```text
PUT    /api/roles/{roleId}/permissions/{permissionId}
DELETE /api/roles/{roleId}/permissions/{permissionId}
```

## 5. Bao mat PasswordHash

Khi tao user, password khong duoc luu truc tiep. API tao `PasswordHash` va luu gia tri hash vao database.

`PasswordHash` khong duoc dua vao response DTO. JSON tra ve user chi gom cac thong tin cong khai:

- Email
- DisplayName
- IsActive
- CreatedAt
- RoleNames

Vi vay thong tin password khong bi lo ra qua API response.

## 6. Migration va chay chuong trinh

Tao hoac cap nhat database bang lenh:

```powershell
dotnet ef database update
```

Chay API bang lenh:

```powershell
dotnet run --urls http://localhost:5080
```

Mo Swagger tai:

```text
http://localhost:5080/swagger
```

Database local cua bai dang chay tren PostgreSQL port `5433`, database ten `week1_rbac`.

## 7. Cac endpoint chinh

### Users

```text
GET    /api/users
GET    /api/users/{id}
POST   /api/users
PUT    /api/users/{id}
DELETE /api/users/{id}
```

### Roles

```text
GET    /api/roles
GET    /api/roles/{id}
POST   /api/roles
PUT    /api/roles/{id}
DELETE /api/roles/{id}
```

### Permissions

```text
GET    /api/permissions
GET    /api/permissions/{id}
POST   /api/permissions
PUT    /api/permissions/{id}
DELETE /api/permissions/{id}
```

## 8. Cau hoi thay co the hoi

### RBAC la gi?

RBAC la mo hinh phan quyen trong do permission duoc gan cho role, sau do user duoc gan role. User khong can duoc gan tung permission truc tiep.

### Vi sao can bang `user_roles`?

Mot user co the co nhieu role va mot role co the duoc gan cho nhieu user. Do la quan he many-to-many nen can bang trung gian `user_roles`.

### Vi sao can bang `role_permissions`?

Mot role co the co nhieu permission va mot permission co the thuoc nhieu role. Vi vay can bang trung gian `role_permissions`.

### Vi sao khong luu password truc tiep?

Neu database bi lo, password dang plain text se bi lo ngay lap tuc. Vi vay password phai duoc hash truoc khi luu.

### Vi sao `PasswordHash` khong xuat hien trong JSON?

API su dung response DTO rieng. `UserResponse` khong co truong `PasswordHash`, nen entity database khong bi serialize truc tiep ra response.

### Unique key co tac dung gi?

Unique key ngan du lieu bi trung. Vi du khong the co hai user cung email, hai role cung ten hoac hai permission cung code.

### Composite key la gi?

Composite key la khoa gom nhieu cot. Vi du `(UserId, RoleId)` dam bao mot user khong bi gan trung cung mot role.

### Migration dung de lam gi?

Migration giup chuyen doi model C# thanh schema database mot cach co kiem soat va luu lai lich su thay doi database.

### Swagger dung de lam gi?

Swagger tu dong tao tai lieu API, hien thi endpoint, request, response va cho phep test API truc tiep tren trinh duyet.

### Bai da co JWT hoac login chua?

Chua. Pham vi Week 1 tap trung vao model RBAC, CRUD va quan he giua cac bang. Authentication bang JWT va authorization middleware co the trien khai o giai doan tiep theo.

### Neu xoa role thi cac quan he lien quan the nao?

Cac ban ghi trong `user_roles` va `role_permissions` lien quan se duoc xoa theo nho cau hinh cascade delete.

## 9. Ket luan

Bai da hoan thanh cac yeu cau chinh:

- Xay dung day du model RBAC.
- Co 5 bang database.
- Co migration va database update.
- Co CRUD cho user, role va permission.
- Co endpoint gan role cho user.
- Co endpoint gan permission cho role.
- Co Swagger de kiem thu API.
- Khong tra `PasswordHash` trong JSON response.
