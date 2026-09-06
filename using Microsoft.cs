using Microsoft.EntityFrameworkCore;
using WarehouseManagement.Data;
using WarehouseManagement.Domain;

namespace WarehouseManagement.Services;

public sealed class InventoryService(WarehouseDbContext db)
{
    // =========================================================
    // GOODS RECEIPT
    // =========================================================

    public async Task<Receipt> CreateReceiptAsync(
        int warehouseId,
        int userId,
        IReadOnlyCollection<MovementLine> lines,
        CancellationToken cancellationToken = default)
    {
        ValidateLines(lines);

        await EnsureActiveWarehouseAsync(
            warehouseId,
            cancellationToken);

        await EnsureProductsAsync(
            lines,
            cancellationToken);

        var receipt = new Receipt
        {
            Number = GenerateDocumentNumber("GR"),
            WarehouseId = warehouseId,
            CreatedBy = userId,
            Lines = lines
                .Select(x => new ReceiptLine
                {
                    ProductId = x.ProductId,
                    Quantity = x.Quantity
                })
                .ToList()
        };

        db.Receipts.Add(receipt);

        await db.SaveChangesAsync(cancellationToken);

        return receipt;
    }


    // =========================================================
    // GOODS ISSUE
    // =========================================================

    public async Task<Issue> CreateIssueAsync(
        int warehouseId,
        int userId,
        IReadOnlyCollection<MovementLine> lines,
        CancellationToken cancellationToken = default)
    {
        ValidateLines(lines);

        await EnsureActiveWarehouseAsync(
            warehouseId,
            cancellationToken);

        await EnsureProductsAsync(
            lines,
            cancellationToken);

        var issue = new Issue
        {
            Number = GenerateDocumentNumber("GI"),
            WarehouseId = warehouseId,
            CreatedBy = userId,
            Lines = lines
                .Select(x => new IssueLine
                {
                    ProductId = x.ProductId,
                    Quantity = x.Quantity
                })
                .ToList()
        };

        db.Issues.Add(issue);

        await db.SaveChangesAsync(cancellationToken);

        return issue;
    }


    // =========================================================
    // APPROVE RECEIPT
    // =========================================================

    public async Task ApproveReceiptAsync(
        int id,
        int approverId,
        CancellationToken cancellationToken = default)
    {
        await using var transaction =
            await db.Database.BeginTransactionAsync(
                cancellationToken);

        var receipt = await db.Receipts
            .Include(x => x.Lines)
            .SingleOrDefaultAsync(
                x => x.Id == id,
                cancellationToken)
            ?? throw new InvalidOperationException(
                "Không tìm thấy phiếu nhập.");

        EnsurePending(receipt.Status);

        EnsureNotSelfApproval(
            receipt.CreatedBy,
            approverId);

        if (receipt.Lines.Count == 0)
        {
            throw new InvalidOperationException(
                "Phiếu nhập không có sản phẩm.");
        }

        var productIds = receipt.Lines
            .Select(x => x.ProductId)
            .Distinct()
            .ToArray();

        var inventories = await db.Inventories
            .Where(x =>
                x.WarehouseId == receipt.WarehouseId &&
                productIds.Contains(x.ProductId))
            .ToDictionaryAsync(
                x => x.ProductId,
                cancellationToken);

        foreach (var line in receipt.Lines)
        {
            ApplyMovement(
                inventories,
                receipt.WarehouseId,
                line.ProductId,
                line.Quantity,
                MovementType.Receipt,
                "GoodsReceipt",
                id,
                approverId);
        }

        receipt.Status = DocumentStatus.Approved;
        receipt.ApprovedBy = approverId;
        receipt.ApprovedAt = DateTime.UtcNow;

        AddAuditLog(
            approverId,
            "APPROVE",
            "GoodsReceipt",
            id);

        await db.SaveChangesAsync(cancellationToken);

        await transaction.CommitAsync(cancellationToken);
    }


    // =========================================================
    // APPROVE ISSUE
    // =========================================================

    public async Task ApproveIssueAsync(
        int id,
        int approverId,
        CancellationToken cancellationToken = default)
    {
        await using var transaction =
            await db.Database.BeginTransactionAsync(
                cancellationToken);

        var issue = await db.Issues
            .Include(x => x.Lines)
            .SingleOrDefaultAsync(
                x => x.Id == id,
                cancellationToken)
            ?? throw new InvalidOperationException(
                "Không tìm thấy phiếu xuất.");

        EnsurePending(issue.Status);

        EnsureNotSelfApproval(
            issue.CreatedBy,
            approverId);

        if (issue.Lines.Count == 0)
        {
            throw new InvalidOperationException(
                "Phiếu xuất không có sản phẩm.");
        }

        var productIds = issue.Lines
            .Select(x => x.ProductId)
            .Distinct()
            .ToArray();

        var inventories = await db.Inventories
            .Where(x =>
                x.WarehouseId == issue.WarehouseId &&
                productIds.Contains(x.ProductId))
            .ToDictionaryAsync(
                x => x.ProductId,
                cancellationToken);

        foreach (var line in issue.Lines)
        {
            ApplyMovement(
                inventories,
                issue.WarehouseId,
                line.ProductId,
                -line.Quantity,
                MovementType.Issue,
                "GoodsIssue",
                id,
                approverId);
        }

        issue.Status = DocumentStatus.Approved;
        issue.ApprovedBy = approverId;
        issue.ApprovedAt = DateTime.UtcNow;

        AddAuditLog(
            approverId,
            "APPROVE",
            "GoodsIssue",
            id);

        await db.SaveChangesAsync(cancellationToken);

        await transaction.CommitAsync(cancellationToken);
    }


    // =========================================================
    // INVENTORY
    // =========================================================

    public Task<List<InventoryView>> GetInventoryAsync(
        int? warehouseId,
        string? sku,
        CancellationToken cancellationToken = default)
    {
        var query = db.Inventories
            .AsNoTracking()
            .Where(x =>
                !warehouseId.HasValue ||
                x.WarehouseId == warehouseId.Value);

        if (!string.IsNullOrWhiteSpace(sku))
        {
            sku = sku.Trim();

            query = query.Where(x =>
                x.Product != null &&
                x.Product.Sku == sku);
        }

        return query
            .OrderBy(x => x.Product!.Sku)
            .Select(x => new InventoryView(
                x.WarehouseId,
                x.Product!.Sku,
                x.Product.Name,
                x.Quantity,
                x.MinStock,
                x.Quantity <= x.MinStock))
            .ToListAsync(cancellationToken);
    }


    // =========================================================
    // STOCK TRANSACTIONS
    // =========================================================

    public Task<List<StockTransaction>> GetTransactionsAsync(
        int? warehouseId,
        int? productId,
        CancellationToken cancellationToken = default)
    {
        var query = db.StockTransactions
            .AsNoTracking()
            .AsQueryable();

        if (warehouseId.HasValue)
        {
            query = query.Where(x =>
                x.WarehouseId == warehouseId.Value);
        }

        if (productId.HasValue)
        {
            query = query.Where(x =>
                x.ProductId == productId.Value);
        }

        return query
            .OrderByDescending(x => x.CreatedAt)
            .Take(200)
            .ToListAsync(cancellationToken);
    }


    // =========================================================
    // PRODUCTS
    // =========================================================

    public Task<List<Product>> GetProductsAsync(
        CancellationToken cancellationToken = default)
    {
        return db.Products
            .AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.Sku)
            .ToListAsync(cancellationToken);
    }


    // =========================================================
    // WAREHOUSES
    // =========================================================

    public Task<List<Warehouse>> GetWarehousesAsync(
        CancellationToken cancellationToken = default)
    {
        return db.Warehouses
            .AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.Code)
            .ToListAsync(cancellationToken);
    }


    // =========================================================
    // PENDING RECEIPTS
    // =========================================================

    public Task<List<Receipt>> GetPendingReceiptsAsync(
        CancellationToken cancellationToken = default)
    {
        return db.Receipts
            .AsNoTracking()
            .Include(x => x.Lines)
            .Where(x => x.Status == DocumentStatus.Pending)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
    }


    // =========================================================
    // PENDING ISSUES
    // =========================================================

    public Task<List<Issue>> GetPendingIssuesAsync(
        CancellationToken cancellationToken = default)
    {
        return db.Issues
            .AsNoTracking()
            .Include(x => x.Lines)
            .Where(x => x.Status == DocumentStatus.Pending)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
    }


    // =========================================================
    // CREATE STOCKTAKE
    // =========================================================

    public async Task<Stocktake> CreateStocktakeAsync(
        int warehouseId,
        int userId,
        IReadOnlyCollection<int>? productIds,
        CancellationToken cancellationToken = default)
    {
        await EnsureActiveWarehouseAsync(
            warehouseId,
            cancellationToken);

        int[] ids;

        if (productIds is { Count: > 0 })
        {
            ids = productIds
                .Distinct()
                .ToArray();
        }
        else
        {
            ids = await db.Inventories
                .AsNoTracking()
                .Where(x => x.WarehouseId == warehouseId)
                .Select(x => x.ProductId)
                .ToArrayAsync(cancellationToken);
        }

        if (ids.Length == 0)
        {
            throw new InvalidOperationException(
                "Kho chưa có SKU để kiểm kê.");
        }

        var inventories = await db.Inventories
            .Where(x =>
                x.WarehouseId == warehouseId &&
                ids.Contains(x.ProductId))
            .ToListAsync(cancellationToken);

        if (inventories.Count == 0)
        {
            throw new InvalidOperationException(
                "Kho chưa có SKU để kiểm kê.");
        }

        var stocktake = new Stocktake
        {
            Number = GenerateDocumentNumber("ST"),
            WarehouseId = warehouseId,
            CreatedBy = userId,
            Details = inventories
                .Select(x => new StocktakeDetail
                {
                    ProductId = x.ProductId,
                    SystemQuantity = x.Quantity
                })
                .ToList()
        };

        db.Stocktakes.Add(stocktake);

        await db.SaveChangesAsync(cancellationToken);

        return stocktake;
    }


    // =========================================================
    // RECORD STOCKTAKE COUNT
    // =========================================================

    public async Task<StocktakeDetail> RecordCountAsync(
        int stocktakeId,
        int detailId,
        int actualQuantity,
        string? reason,
        CancellationToken cancellationToken = default)
    {
        if (actualQuantity < 0)
        {
            throw new InvalidOperationException(
                "Số thực tế không được âm.");
        }

        var detail = await db.StocktakeDetails
            .Include(x => x.Stocktake)
            .SingleOrDefaultAsync(
                x =>
                    x.Id == detailId &&
                    x.StocktakeId == stocktakeId,
                cancellationToken)
            ?? throw new InvalidOperationException(
                "Không tìm thấy dòng kiểm kê.");

        if (detail.Stocktake!.Status == StocktakeStatus.Closed)
        {
            throw new InvalidOperationException(
                "Đợt kiểm kê không còn mở.");
        }

        detail.ActualQuantity = actualQuantity;
        detail.Difference =
            actualQuantity - detail.SystemQuantity;

        detail.Reason =
            string.IsNullOrWhiteSpace(reason)
                ? null
                : reason.Trim();

        detail.Stocktake.Status =
            StocktakeStatus.Review;

        await db.SaveChangesAsync(cancellationToken);

        return detail;
    }


    // =========================================================
    // GET STOCKTAKE
    // =========================================================

    public Task<Stocktake?> GetStocktakeAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        return db.Stocktakes
            .AsNoTracking()
            .Include(x => x.Details)
            .ThenInclude(x => x.Product)
            .SingleOrDefaultAsync(
                x => x.Id == id,
                cancellationToken);
    }


    // =========================================================
    // APPROVE STOCKTAKE
    // =========================================================

    public async Task ApproveStocktakeAsync(
        int stocktakeId,
        int approverId,
        CancellationToken cancellationToken = default)
    {
        await using var transaction =
            await db.Database.BeginTransactionAsync(
                cancellationToken);

        var stocktake = await db.Stocktakes
            .Include(x => x.Details)
            .SingleOrDefaultAsync(
                x => x.Id == stocktakeId,
                cancellationToken)
            ?? throw new InvalidOperationException(
                "Không tìm thấy đợt kiểm kê.");

        if (stocktake.Status == StocktakeStatus.Closed)
        {
            throw new InvalidOperationException(
                "Đợt kiểm kê đã đóng.");
        }

        if (stocktake.Details.Count == 0)
        {
            throw new InvalidOperationException(
                "Đợt kiểm kê không có sản phẩm.");
        }

        if (stocktake.Details.Any(
                x => x.ActualQuantity is null))
        {
            throw new InvalidOperationException(
                "Chưa nhập đủ số thực tế.");
        }

        var productIds = stocktake.Details
            .Select(x => x.ProductId)
            .Distinct()
            .ToArray();

        var inventories = await db.Inventories
            .Where(x =>
                x.WarehouseId == stocktake.WarehouseId &&
                productIds.Contains(x.ProductId))
            .ToDictionaryAsync(
                x => x.ProductId,
                cancellationToken);

        foreach (var detail in stocktake.Details)
        {
            var difference =
                detail.ActualQuantity!.Value -
                detail.SystemQuantity;

            detail.Difference = difference;

            if (difference == 0)
            {
                detail.IsAdjusted = true;
                continue;
            }

            if (string.IsNullOrWhiteSpace(detail.Reason))
            {
                throw new InvalidOperationException(
                    "Dòng chênh lệch bắt buộc có lý do.");
            }

            ApplyMovement(
                inventories,
                stocktake.WarehouseId,
                detail.ProductId,
                difference,
                MovementType.Adjustment,
                "Stocktake",
                stocktakeId,
                approverId);

            detail.IsAdjusted = true;
        }

        stocktake.Status =
            StocktakeStatus.Closed;

        stocktake.ApprovedBy =
            approverId;

        AddAuditLog(
            approverId,
            "ADJUST_STOCK",
            "Stocktake",
            stocktakeId);

        await db.SaveChangesAsync(cancellationToken);

        await transaction.CommitAsync(cancellationToken);
    }


    // =========================================================
    // REPORT
    // =========================================================

    public async Task<List<ReportRow>> GetReportAsync(
        DateTime? from,
        DateTime? to,
        int? warehouseId,
        CancellationToken cancellationToken = default)
    {
        var startDate =
            from?.Date;

        var endDate =
            to?.Date.AddDays(1)
            ?? DateTime.UtcNow;

        var transactionQuery =
            db.StockTransactions
                .AsNoTracking()
                .Where(x =>
                    x.CreatedAt < endDate);

        if (startDate.HasValue)
        {
            transactionQuery = transactionQuery
                .Where(x =>
                    x.CreatedAt >= startDate.Value);
        }

        if (warehouseId.HasValue)
        {
            transactionQuery = transactionQuery
                .Where(x =>
                    x.WarehouseId == warehouseId.Value);
        }

        // QUAN TRỌNG:
        // Group theo WarehouseId + ProductId để tránh
        // trộn giao dịch giữa các kho.
        var movements = await transactionQuery
            .GroupBy(x => new
            {
                x.WarehouseId,
                x.ProductId
            })
            .Select(g => new MovementSummary
            {
                WarehouseId = g.Key.WarehouseId,
                ProductId = g.Key.ProductId,

                ReceiptQuantity = g
                    .Where(x =>
                        x.Type == MovementType.Receipt)
                    .Sum(x => x.ChangeQuantity),

                IssueQuantity = -g
                    .Where(x =>
                        x.Type == MovementType.Issue)
                    .Sum(x => x.ChangeQuantity),

                AdjustmentQuantity = g
                    .Where(x =>
                        x.Type == MovementType.Adjustment)
                    .Sum(x => x.ChangeQuantity)
            })
            .ToListAsync(cancellationToken);

        var inventoryQuery =
            db.Inventories
                .AsNoTracking()
                .Where(x =>
                    x.Product != null);

        if (warehouseId.HasValue)
        {
            inventoryQuery = inventoryQuery
                .Where(x =>
                    x.WarehouseId == warehouseId.Value);
        }

        var inventory = await inventoryQuery
            .Select(x => new InventorySummary
            {
                WarehouseId = x.WarehouseId,
                ProductId = x.ProductId,
                Sku = x.Product!.Sku,
                ProductName = x.Product.Name,
                Quantity = x.Quantity
            })
            .ToListAsync(cancellationToken);

        var result = inventory
            .GroupJoin(
                movements,

                inventoryItem =>
                    new
                    {
                        inventoryItem.WarehouseId,
                        inventoryItem.ProductId
                    },

                movement =>
                    new
                    {
                        movement.WarehouseId,
                        movement.ProductId
                    },

                (item, changes) =>
                {
                    var change =
                        changes.SingleOrDefault();

                    var receipt =
                        change?.ReceiptQuantity ?? 0;

                    var issue =
                        change?.IssueQuantity ?? 0;

                    var adjustment =
                        change?.AdjustmentQuantity ?? 0;

                    var opening =
                        item.Quantity
                        - receipt
                        + issue
                        - adjustment;

                    return new ReportRow(
                        item.Sku,
                        item.ProductName,
                        opening,
                        receipt,
                        issue,
                        adjustment,
                        item.Quantity);
                })
            .OrderBy(x => x.Sku)
            .ToList();

        return result;
    }


    // =========================================================
    // APPLY MOVEMENT
    // =========================================================

    private void ApplyMovement(
        IReadOnlyDictionary<int, Inventory> inventories,
        int warehouseId,
        int productId,
        int change,
        MovementType type,
        string referenceType,
        int referenceId,
        int userId)
    {
        if (!inventories.TryGetValue(
                productId,
                out var inventory))
        {
            throw new InvalidOperationException(
                "SKU chưa được cấu hình trong kho.");
        }

        var before =
            inventory.Quantity;

        var after =
            checked(before + change);

        if (after < 0)
        {
            throw new InvalidOperationException(
                "Không đủ tồn khả dụng để duyệt phiếu xuất.");
        }

        inventory.Quantity =
            after;

        db.StockTransactions.Add(
            new StockTransaction
            {
                WarehouseId = warehouseId,
                ProductId = productId,
                Type = type,

                BeforeQuantity =
                    before,

                ChangeQuantity =
                    change,

                AfterQuantity =
                    after,

                ReferenceType =
                    referenceType,

                ReferenceId =
                    referenceId,

                CreatedBy =
                    userId
            });
    }


    // =========================================================
    // VALIDATION
    // =========================================================

    private async Task EnsureProductsAsync(
        IEnumerable<MovementLine> lines,
        CancellationToken cancellationToken)
    {
        var ids = lines
            .Select(x => x.ProductId)
            .Distinct()
            .ToArray();

        if (ids.Length == 0)
        {
            throw new InvalidOperationException(
                "Danh sách SKU không được rỗng.");
        }

        var activeProductCount =
            await db.Products
                .CountAsync(
                    x =>
                        ids.Contains(x.Id) &&
                        x.IsActive,
                    cancellationToken);

        if (activeProductCount != ids.Length)
        {
            throw new InvalidOperationException(
                "SKU không tồn tại hoặc đã bị khóa.");
        }
    }


    private async Task EnsureActiveWarehouseAsync(
        int id,
        CancellationToken cancellationToken)
    {
        var exists =
            await db.Warehouses
                .AnyAsync(
                    x =>
                        x.Id == id &&
                        x.IsActive,
                    cancellationToken);

        if (!exists)
        {
            throw new InvalidOperationException(
                "Kho không tồn tại hoặc đã bị khóa.");
        }
    }


    private static void ValidateLines(
        IReadOnlyCollection<MovementLine> lines)
    {
        if (lines.Count == 0)
        {
            throw new InvalidOperationException(
                "Danh sách SKU không được rỗng.");
        }

        if (lines.Any(x => x.Quantity <= 0))
        {
            throw new InvalidOperationException(
                "Số lượng phải lớn hơn 0.");
        }

        if (lines.Any(x => x.ProductId <= 0))
        {
            throw new InvalidOperationException(
                "ProductId không hợp lệ.");
        }
    }


    private static void EnsurePending(
        DocumentStatus status)
    {
        if (status != DocumentStatus.Pending)
        {
            throw new InvalidOperationException(
                "Chứng từ không ở trạng thái chờ duyệt.");
        }
    }


    private static void EnsureNotSelfApproval(
        int creatorId,
        int approverId)
    {
        if (creatorId == approverId)
        {
            throw new InvalidOperationException(
                "Người tạo không được tự duyệt chứng từ.");
        }
    }


    // =========================================================
    // AUDIT LOG
    // =========================================================

    private void AddAuditLog(
        int userId,
        string action,
        string entityType,
        int entityId)
    {
        db.AuditLogs.Add(
            new AuditLog
            {
                UserId = userId,
                Action = action,
                EntityType = entityType,
                EntityId = entityId
            });
    }


    // =========================================================
    // DOCUMENT NUMBER
    // =========================================================

    private static string GenerateDocumentNumber(
        string prefix)
    {
        return $"{prefix}-{DateTime.UtcNow:yyyyMMddHHmmssfff}";
    }
}


// =============================================================
// SUPPORTING RECORDS
// =============================================================

public record MovementLine(
    int ProductId,
    int Quantity);


public record InventoryView(
    int WarehouseId,
    string Sku,
    string ProductName,
    int Quantity,
    int MinStock,
    bool LowStock);


public record ReportRow(
    string Sku,
    string ProductName,
    int OpeningQuantity,
    int ReceiptQuantity,
    int IssueQuantity,
    int AdjustmentQuantity,
    int ClosingQuantity);


// =============================================================
// INTERNAL REPORT MODELS
// =============================================================

internal sealed class MovementSummary
{
    public int WarehouseId { get; init; }

    public int ProductId { get; init; }

    public int ReceiptQuantity { get; init; }

    public int IssueQuantity { get; init; }

    public int AdjustmentQuantity { get; init; }
}


internal sealed class InventorySummary
{
    public int WarehouseId { get; init; }

    public int ProductId { get; init; }

    public string Sku { get; init; } = string.Empty;

    public string ProductName { get; init; } = string.Empty;

    public int Quantity { get; init; }
}