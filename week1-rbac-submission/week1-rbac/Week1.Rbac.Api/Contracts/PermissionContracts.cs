using System.ComponentModel.DataAnnotations;

namespace Week1.Rbac.Api.Contracts;

public sealed record CreateCategoryRequest(
    [property: Required, MinLength(1), MaxLength(100)] string Code,
    [property: Required, MinLength(2), MaxLength(150)] string Name,
    [property: MaxLength(500)] string? Description);

public sealed record UpdateCategoryRequest(
    [property: Required, MinLength(1), MaxLength(100)] string Code,
    [property: Required, MinLength(2), MaxLength(150)] string Name,
    [property: MaxLength(500)] string? Description);

public sealed record CategoryResponse(
    Guid Id,
    string Code,
    string Name,
    string? Description,
    DateTimeOffset CreatedAt,
    int ProductCount);
