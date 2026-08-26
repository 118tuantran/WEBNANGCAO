# Cấu trúc project

## Tổng quan

```text
BTLWEB/
|-- Controllers/                 # HTTP API và phân quyền endpoint
|   |-- AdminController.cs       # Danh mục, kho và audit dành cho Admin
|   |-- AuthController.cs        # Đăng nhập, đăng xuất, cookie session
|   |-- ReportController.cs      # Báo cáo nhập - xuất - tồn
|   |-- StocktakeController.cs   # Kiểm kê, nhập số thực tế, duyệt điều chỉnh
|   `-- WarehouseController.cs   # Tồn kho, giao dịch, phiếu nhập/xuất
|-- Data/
|   `-- WarehouseDbContext.cs    # EF Core DbSet, index, quan hệ, dữ liệu mẫu
|-- Domain/
|   `-- Models.cs                # Entity và enum của nghiệp vụ kho
|-- Services/
|   `-- InventoryService.cs      # Luật tồn kho và workflow nghiệp vụ
|-- wwwroot/
|   `-- index.html               # Giao diện web responsive chạy tại `/`
|-- Program.cs                   # Điểm khởi động và cấu hình ứng dụng
|-- WarehouseManagement.csproj   # Target framework và package NuGet
|-- README.md                    # Hướng dẫn chạy, tài khoản, API
|-- CONTRIBUTING.md              # Quy ước branch, commit, Pull Request
|-- PROJECT-STRUCTURE.md         # Tài liệu file này
`-- .gitignore                   # File không được commit lên GitHub
```

## Luồng xử lý chính

`Controller -> InventoryService -> WarehouseDbContext -> SQLite`

- Controller nhận request, kiểm tra role và trả HTTP status.
- Service kiểm tra business rule, mở DB transaction và cập nhật tồn.
- DbContext định nghĩa dữ liệu, index, khóa duy nhất và seed data.
- `Inventory` là trạng thái hiện tại; `StockTransaction` là lịch sử bất biến.
- Giao diện chỉ gọi API, không tự cập nhật số tồn.

## Các nguyên tắc cần giữ

- Chỉ chứng từ `Pending` mới được duyệt.
- Người tạo không được tự duyệt.
- Không cho tồn âm.
- Cập nhật `Inventory`, `StockTransaction` và trạng thái chứng từ trong cùng transaction.
- Không commit `warehouse.db`, `bin/` hoặc `obj/`.