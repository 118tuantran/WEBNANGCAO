# Warehouse Management System - Setup Guide

## Project Conversion Summary

This project has been converted from RBAC (Role-Based Access Control) to a **Warehouse Management System** backend.

## Key Changes

### Models
- `User` → `Product` (with category and supplier relationships)
- `Role` → `Warehouse` (with inventory tracking)
- `Permission` → `Category` (product classification)
- `UserRole` → `Inventory` (warehouse × product M2M)
- `RolePermission` → `ProductSupplier` (product × supplier M2M)
- `+ Supplier` (new model for vendors)

### API Endpoints
- `/api/products` - Product management
- `/api/categories` - Product categories
- `/api/warehouses` - Warehouse inventory management
- `/api/suppliers` - Supplier management
- `/api/inventories` - Stock tracking

### Database Changes
- Database name: `week1_warehouse` (was `week1_rbac`)
- New schema with warehouse, product, category, supplier, and inventory tables

## Setup Instructions

### 1. Database Setup (First Time)

Delete the old `week1_rbac` database and create the new one:

```sql
DROP DATABASE IF EXISTS week1_rbac;
CREATE DATABASE week1_warehouse OWNER week1_app;
```

If user/role doesn't exist:
```sql
CREATE USER week1_app WITH PASSWORD 'week1_secret';
CREATE DATABASE week1_warehouse OWNER week1_app;
```

### 2. Clean Up Old Migrations (Optional but Recommended)

The old migration file `20260812032219_InitialRbac.cs` and its Designer file are no longer needed. You can safely delete them from the `Migrations/` folder:

- Delete: `Migrations/20260812032219_InitialRbac.cs`
- Delete: `Migrations/20260812032219_InitialRbac.Designer.cs`

The new migration `20260812032220_InitialWarehouseManagement.cs` will be used instead.

### 3. Run the Application

Using the helper script:
```batch
run.bat
```

Or manually from the `week1-rbac` folder:
```powershell
dotnet tool restore
dotnet restore .\Week1.Rbac.Api\Week1.Rbac.Api.csproj
dotnet tool run dotnet-ef --project .\Week1.Rbac.Api\Week1.Rbac.Api.csproj database update
cd .\Week1.Rbac.Api
dotnet run --urls http://localhost:5080
```

### 4. Test the API

Open Swagger at: `http://localhost:5080/swagger`

#### Sample Test Flow:
1. POST `/api/categories` - Create category "Electronics"
2. POST `/api/products` - Create product "LAPTOP-001" with price 15000000
3. POST `/api/warehouses` - Create warehouse "WH-01"
4. POST `/api/suppliers` - Create supplier "SUPP-001"
5. PUT `/api/products/{productId}/suppliers/{supplierId}` - Assign supplier
6. PUT `/api/warehouses/{warehouseId}/products/{productId}` - Add inventory (50 units)
7. GET `/api/warehouses/{warehouseId}` - Verify inventory
8. GET `/api/products/{productId}` - Verify category and suppliers

## API Documentation

See [README.md](./README.md) for complete API endpoint documentation.

## Notes

- All endpoints are currently anonymous (no authentication)
- No authorization checks implemented
- Database is reset with new schema - all old data is lost
- Connection string: `Host=localhost;Port=5432;Database=week1_warehouse;Username=week1_app;Password=week1_secret`
