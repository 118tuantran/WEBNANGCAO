using System.ComponentModel.DataAnnotations;

namespace Week1.Rbac.Api.Contracts;

public sealed record CreateSupplierRequest(
    [property: Required, MinLength(1), MaxLength(100)] string Code,
    [property: Required, MinLength(2), MaxLength(256)] string Name,
    [property: Phone, MaxLength(20)] string? Contact,
    [property: EmailAddress, MaxLength(256)] string? Email,
    [property: MaxLength(500)] string? Address);

public sealed record UpdateSupplierRequest(
    [property: Required, MinLength(1), MaxLength(100)] string Code,
    [property: Required, MinLength(2), MaxLength(256)] string Name,
    [property: Phone, MaxLength(20)] string? Contact,
    [property: EmailAddress, MaxLength(256)] string? Email,
    [property: MaxLength(500)] string? Address,
    bool IsActive);

public sealed record SupplierResponse(
    Guid Id,
    string Code,
    string Name,
    string? Contact,
    string? Email,
    string? Address,
    bool IsActive,
    DateTimeOffset CreatedAt,
    IReadOnlyCollection<string> ProductCodes);
