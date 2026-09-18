# Báo cáo kiểm thử chức năng

Ngày kiểm thử: 18/09/2026
Môi trường: Windows, ASP.NET Core .NET 10, MySQL XAMPP `127.0.0.1:3306`, database `webnangcao`
Ứng dụng: `http://localhost:5165`

## 1. Kết quả tổng hợp

| Nhóm kiểm thử | Kết quả |
|---|---|
| Build project | PASS |
| Unit test nghiệp vụ | PASS: 20/20 |
| Đăng nhập 3 vai trò | PASS |
| Phân quyền API | PASS |
| Workflow nhập/xuất thực tế | PASS |
| Kiểm tra database MySQL/phpMyAdmin | PASS |
| Health check | PASS: HTTP 200 |
| Swagger/OpenAPI | PASS: HTTP 200 |
| UI responsive | PASS ở desktop/tablet/mobile |

## 2. Tài khoản và vai trò

| Vai trò | Tài khoản | Kết quả |
|---|---|---|
| Admin | `admin / Admin@123` | Đăng nhập PASS |
| Quản lý kho | `manager / Manager@123` | Đăng nhập PASS |
| Nhân viên kho | `staff / Staff@123` | Đăng nhập PASS |

## 3. Kiểm thử phân quyền thực tế

| Kịch bản | Expected | Actual | Kết quả |
|---|---:|---:|---|
| Manager xem master data | 200 | 200 | PASS |
| Admin truy cập master data của Manager | 403 | 403 | PASS |
| Manager xem tồn kho | 200 | 200 | PASS |
| Staff xem tồn kho | 200 | 200 | PASS |
| Admin truy cập báo cáo kho | 403 | 403 | PASS |
| Manager xem báo cáo kho | 200 | 200 | PASS |
| Staff truy cập danh sách phiếu chờ duyệt | 403 | 403 | PASS |
| Admin xem Audit Log | 200 | 200 | PASS |
| Manager xem Audit Log | 200 | 200 | PASS |

## 4. Kiểm thử workflow nhập/xuất

1. Staff đăng nhập thành công.
2. Staff tạo phiếu nhập cho `SKU-001`, số lượng `2`, đơn giá `125000`.
3. Manager duyệt phiếu nhập thành công.
4. Staff tạo phiếu xuất cho `SKU-001`, số lượng `1`.
5. Manager duyệt phiếu xuất thành công.
6. Tồn kho sau workflow: `SKU-001 = 21`.

Kết quả database kiểm tra bằng MySQL/phpMyAdmin:

| Kiểm tra | Kết quả |
|---|---|
| Phiếu nhập | `GR-20260918125001818`, Status `2` = Approved |
| UnitCost phiếu nhập | `125000` |
| Tồn `SKU-001` | `21` |
| Giao dịch nhập | Before `20`, Change `+2`, After `22` |
| Giao dịch xuất | Before `22`, Change `-1`, After `21` |
| Reference nhập | `GoodsReceipt` |
| Reference xuất | `GoodsIssue` |

## 5. Kiểm tra database trên phpMyAdmin

Database `webnangcao` có các bảng chính:

- `users`
- `roles`
- `categories`
- `products`
- `warehouses`
- `suppliers`
- `inventories`
- `receipts`
- `receiptlines`
- `issues`
- `issuelines`
- `stocktakes`
- `stocktakedetails`
- `stocktransactions`
- `auditlogs`
- `__efmigrationshistory`

Schema đã được quản lý bằng EF Core migration `InitialMySqlSchema`. Ứng dụng khởi động bằng `MigrateAsync()` và log xác nhận database đã up to date.

Có thể xem database tại:

- phpMyAdmin: `http://localhost/phpmyadmin`
- Database: `webnangcao`

## 6. Kiểm thử nghiệp vụ tự động

Lệnh đã chạy:

```powershell
dotnet test .\Tests\WarehouseManagement.Tests.csproj --nologo
```

Kết quả:

- Total: 20
- Passed: 20
- Failed: 0
- Skipped: 0

Các nhóm được kiểm thử gồm: duyệt nhập, duyệt xuất, chặn vượt tồn, chống duyệt hai lần, SKU unique, tra cứu tồn, cảnh báo tồn thấp, kiểm kê, điều chỉnh tồn, báo cáo và audit.

## 7. Kiểm thử API kỹ thuật

- `/health`: HTTP 200.
- `/swagger/index.html`: HTTP 200.
- API smoke test: PASS.
- Latency truy vấn sản phẩm trong smoke test: trung bình khoảng `18.52 ms` cho 20 lần gọi.

Lệnh smoke test:

```powershell
powershell -ExecutionPolicy Bypass -File .\Tests\api-smoke.ps1
```

## 8. Kiểm thử giao diện responsive

Playwright đã kiểm tra màn hình ở:

- Desktop: `1440 x 900`
- Tablet: `900 x 1024`
- Mobile: `390 x 844`

Kết quả:

- Không tràn ngang.
- Manager thấy tab `Danh mục`.
- Admin thấy màn hình `Quản trị tài khoản`.
- Admin không thấy form master data.
- Manager mở được màn hình danh mục.

## 9. Kết luận đối chiếu file phân tích

Project đã đạt đầy đủ các yêu cầu nghiệp vụ cốt lõi trong file phân tích:

- 3 vai trò và RBAC.
- Quản lý User/Role.
- Quản lý SKU, danh mục, kho và Supplier.
- Phiếu nhập/xuất và quy trình phê duyệt.
- Kiểm soát tồn âm và xuất vượt tồn.
- Kiểm kê và điều chỉnh tồn.
- Cảnh báo tồn thấp.
- Báo cáo nhập-xuất-tồn và export.
- StockTransaction và AuditLog.
- MySQL/XAMPP.
- Migration, validation, Swagger, health check, exception handling.
- Backup/restore script, CI và Docker configuration.
- Kiểm thử unit, API và responsive UI.

## 10. Giới hạn còn lại

- Docker Compose chưa chạy thực tế vì máy kiểm thử chưa có Docker Desktop/daemon.
- Chưa có load test quy mô lớn hoặc kiểm thử concurrency trên production.
- Backup đã tạo và kiểm tra thành công; chưa diễn tập restore trên máy production.
- Cần UAT thêm với người dùng thật trên thiết bị thật.

Đánh giá cuối: **đủ yêu cầu cho bài tập, demo và bảo vệ theo file phân tích**. Các giới hạn còn lại là kiểm thử/vận hành production, không phải thiếu chức năng nghiệp vụ cốt lõi.
