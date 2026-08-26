using System.ComponentModel.DataAnnotations;

namespace Week1.Rbac.Api.Contracts;

public sealed record CreateUserRequest(
    [param: Required, EmailAddress] string Email,
    [param: Required, MinLength(2), MaxLength(120)] string DisplayName,
    [param: Required, MinLength(6)] string Password,
    bool IsActive = true);

public sealed record UpdateUserRequest(
    [param: Required, EmailAddress] string Email,
    [param: Required, MinLength(2), MaxLength(120)] string DisplayName,
    [param: Required] bool IsActive);

public sealed record UserResponse(
    Guid Id,
    string Email,
    string DisplayName,
    bool IsActive,
    DateTimeOffset CreatedAt,
    IReadOnlyCollection<string> RoleNames);

public sealed record CreateRoleRequest(
    [param: Required, MinLength(2), MaxLength(100)] string Name,
    [param: MaxLength(500)] string? Description);

public sealed record UpdateRoleRequest(
    [param: Required, MinLength(2), MaxLength(100)] string Name,
    [param: MaxLength(500)] string? Description);

public sealed record RoleResponse(
    Guid Id,
    string Name,
    string? Description,
    DateTimeOffset CreatedAt,
    IReadOnlyCollection<string> PermissionCodes);

public sealed record CreatePermissionRequest(
    [param: Required, MinLength(2), MaxLength(150)] string Code,
    [param: MaxLength(500)] string? Description);

public sealed record UpdatePermissionRequest(
    [param: Required, MinLength(2), MaxLength(150)] string Code,
    [param: MaxLength(500)] string? Description);

public sealed record PermissionResponse(
    Guid Id,
    string Code,
    string? Description,
    DateTimeOffset CreatedAt);
