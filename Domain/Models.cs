using System.Text.Json.Serialization;

namespace WarehouseManagement.Domain;

public enum DocumentStatus { Draft, Pending, Approved, Rejected }
public enum MovementType { Receipt, Issue, Adjustment }
public enum UserRole { Admin, WarehouseManager, WarehouseStaff }
public enum StocktakeStatus { Open, Review, Closed }

public sealed class User
{
    public int Id { get; set; }
    public string Username { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public UserRole Role { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class Product
{
    public int Id { get; set; }
    public string Sku { get; set; } = "";
    public string Name { get; set; } = "";
    public string Unit { get; set; } = "";
    public bool IsActive { get; set; } = true;
}

public sealed class Warehouse
{
    public int Id { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public bool IsActive { get; set; } = true;
}

public sealed class Inventory
{
    public int Id { get; set; }
    public int WarehouseId { get; set; }
    public int ProductId { get; set; }
    public int Quantity { get; set; }
    public int MinStock { get; set; }
    public Warehouse? Warehouse { get; set; }
    public Product? Product { get; set; }
}

public sealed class Receipt
{
    public int Id { get; set; }
    public string Number { get; set; } = "";
    public int WarehouseId { get; set; }
    public DocumentStatus Status { get; set; } = DocumentStatus.Pending;
    public int CreatedBy { get; set; }
    public int? ApprovedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ApprovedAt { get; set; }
    public List<ReceiptLine> Lines { get; set; } = [];
}

public sealed class ReceiptLine
{
    public int Id { get; set; }
    public int ReceiptId { get; set; }
    public int ProductId { get; set; }
    public int Quantity { get; set; }
}

public sealed class Issue
{
    public int Id { get; set; }
    public string Number { get; set; } = "";
    public int WarehouseId { get; set; }
    public DocumentStatus Status { get; set; } = DocumentStatus.Pending;
    public int CreatedBy { get; set; }
    public int? ApprovedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ApprovedAt { get; set; }
    public List<IssueLine> Lines { get; set; } = [];
}

public sealed class IssueLine
{
    public int Id { get; set; }
    public int IssueId { get; set; }
    public int ProductId { get; set; }
    public int Quantity { get; set; }
}

public sealed class StockTransaction
{
    public long Id { get; set; }
    public int WarehouseId { get; set; }
    public int ProductId { get; set; }
    public MovementType Type { get; set; }
    public int BeforeQuantity { get; set; }
    public int ChangeQuantity { get; set; }
    public int AfterQuantity { get; set; }
    public string ReferenceType { get; set; } = "";
    public int ReferenceId { get; set; }
    public int CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public sealed class AuditLog
{
    public long Id { get; set; }
    public int UserId { get; set; }
    public string Action { get; set; } = "";
    public string EntityType { get; set; } = "";
    public int EntityId { get; set; }
    public string? Metadata { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public sealed class Stocktake
{
    public int Id { get; set; }
    public string Number { get; set; } = "";
    public int WarehouseId { get; set; }
    public StocktakeStatus Status { get; set; } = StocktakeStatus.Open;
    public int CreatedBy { get; set; }
    public int? ApprovedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public List<StocktakeDetail> Details { get; set; } = [];
}

public sealed class StocktakeDetail
{
    public int Id { get; set; }
    public int StocktakeId { get; set; }
    public int ProductId { get; set; }
    public int SystemQuantity { get; set; }
    public int? ActualQuantity { get; set; }
    public int? Difference { get; set; }
    public string? Reason { get; set; }
    public bool IsAdjusted { get; set; }
    public Product? Product { get; set; }
    [JsonIgnore]
    public Stocktake? Stocktake { get; set; }
}
