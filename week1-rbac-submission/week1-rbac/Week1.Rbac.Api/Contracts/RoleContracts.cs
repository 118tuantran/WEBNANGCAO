using System.ComponentModel.DataAnnotations;

namespace Week1.Rbac.Api.Contracts;

public sealed record CreateWarehouseRequest(
    [property: Required, MinLength(1), MaxLength(100)] string Code,
    [property: Required, MinLength(2), MaxLength(150)] string Name,
    [property: MaxLength(256)] string? Location,
    [property: Range(0, 999999999)] decimal Capacity);

public sealed record UpdateWarehouseRequest(
    [property: Required, MinLength(1), MaxLength(100)] string Code,
    [property: Required, MinLength(2), MaxLength(150)] string Name,
    [property: MaxLength(256)] string? Location,
    [property: Range(0, 999999999)] decimal Capacity);

public sealed record WarehouseResponse(
    Guid Id,
    string Code,
    string Name,
    string? Location,
    decimal Capacity,
    DateTimeOffset CreatedAt,
    IReadOnlyCollection<InventoryItemResponse> Inventories);

public sealed record InventoryItemResponse(
    Guid ProductId,
    string ProductCode,
    string ProductName,
    int Quantity,
    DateTimeOffset LastUpdated);
