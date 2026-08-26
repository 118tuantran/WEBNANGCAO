using System.ComponentModel.DataAnnotations;

namespace Week1.Rbac.Api.Contracts;

public sealed record CreateProductRequest(
    [property: Required, MinLength(1), MaxLength(100)] string Code,
    [property: Required, MinLength(2), MaxLength(256)] string Name,
    [property: MaxLength(500)] string? Description,
    [property: Required, Range(0.01, 999999999)] decimal Price,
    [property: Required] Guid CategoryId);

public sealed record UpdateProductRequest(
    [property: Required, MinLength(1), MaxLength(100)] string Code,
    [property: Required, MinLength(2), MaxLength(256)] string Name,
    [property: MaxLength(500)] string? Description,
    [property: Required, Range(0.01, 999999999)] decimal Price,
    [property: Required] Guid CategoryId,
    bool IsActive);

public sealed record ProductResponse(
    Guid Id,
    string Code,
    string Name,
    string? Description,
    decimal Price,
    Guid CategoryId,
    string CategoryName,
    bool IsActive,
    DateTimeOffset CreatedAt,
    IReadOnlyCollection<string> SupplierCodes);
