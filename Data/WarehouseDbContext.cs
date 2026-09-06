using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using WarehouseManagement.Domain;

namespace WarehouseManagement.Data;

public sealed class WarehouseDbContext(DbContextOptions<WarehouseDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Warehouse> Warehouses => Set<Warehouse>();
    public DbSet<Inventory> Inventories => Set<Inventory>();
    public DbSet<Receipt> Receipts => Set<Receipt>();
    public DbSet<ReceiptLine> ReceiptLines => Set<ReceiptLine>();
    public DbSet<Issue> Issues => Set<Issue>();
    public DbSet<IssueLine> IssueLines => Set<IssueLine>();
    public DbSet<StockTransaction> StockTransactions => Set<StockTransaction>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<Stocktake> Stocktakes => Set<Stocktake>();
    public DbSet<StocktakeDetail> StocktakeDetails => Set<StocktakeDetail>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Product>().HasIndex(x => x.Sku).IsUnique();
        modelBuilder.Entity<Category>().HasIndex(x => x.Name).IsUnique();
        modelBuilder.Entity<Warehouse>().HasIndex(x => x.Code).IsUnique();
        modelBuilder.Entity<Inventory>().HasIndex(x => new { x.WarehouseId, x.ProductId }).IsUnique();
        modelBuilder.Entity<Receipt>().HasIndex(x => x.Number).IsUnique();
        modelBuilder.Entity<Issue>().HasIndex(x => x.Number).IsUnique();
        modelBuilder.Entity<Receipt>().HasMany(x => x.Lines).WithOne().HasForeignKey(x => x.ReceiptId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<Issue>().HasMany(x => x.Lines).WithOne().HasForeignKey(x => x.IssueId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<Stocktake>().HasIndex(x => x.Number).IsUnique();
        modelBuilder.Entity<Stocktake>().HasMany(x => x.Details).WithOne(x => x.Stocktake).HasForeignKey(x => x.StocktakeId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<Product>().HasOne(x => x.Category).WithMany().HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<User>().HasData(
            new User { Id = 1, Username = "admin", PasswordHash = Hash("Admin@123"), Role = UserRole.Admin },
            new User { Id = 2, Username = "manager", PasswordHash = Hash("Manager@123"), Role = UserRole.WarehouseManager },
            new User { Id = 3, Username = "staff", PasswordHash = Hash("Staff@123"), Role = UserRole.WarehouseStaff });
        modelBuilder.Entity<Warehouse>().HasData(new Warehouse { Id = 1, Code = "WH-MAIN", Name = "Kho chính" });
        modelBuilder.Entity<Product>().HasData(
            new Product { Id = 1, Sku = "SKU-001", Name = "Bàn phím cơ", Unit = "cái", CategoryId = 1 },
            new Product { Id = 2, Sku = "SKU-002", Name = "Chuột không dây", Unit = "cái", CategoryId = 1 });
        modelBuilder.Entity<Category>().HasData(new Category { Id = 1, Name = "Thiết bị máy tính" });
        modelBuilder.Entity<Inventory>().HasData(
            new Inventory { Id = 1, WarehouseId = 1, ProductId = 1, Quantity = 20, MinStock = 5 },
            new Inventory { Id = 2, WarehouseId = 1, ProductId = 2, Quantity = 8, MinStock = 10 });
    }

    public static string Hash(string value)
    {
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
    }
}
