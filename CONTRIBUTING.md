# Quy ước làm việc nhóm

## Cách lấy project

```powershell
git clone <URL_REPOSITORY>
cd BTLWEB
dotnet restore
dotnet run --urls http://localhost:5077
```

Mở `http://localhost:5077`. Database SQLite `warehouse.db` được tạo tự động và không commit lên GitHub.

## Quy ước branch

- `main`: phiên bản ổn định để demo/nộp bài.
- `develop`: branch tích hợp các tính năng đã review.
- `feature/<ten-chuc-nang>`: branch cho từng chức năng, ví dụ `feature/stocktake`.
- `fix/<ten-loi>`: branch sửa lỗi.

Không commit trực tiếp vào `main`. Mỗi thay đổi cần Pull Request và ít nhất một thành viên review.

## Quy trình một task

1. Kéo code mới nhất từ `develop`.
2. Tạo branch riêng cho task.
3. Thực hiện thay đổi nhỏ, build bằng `dotnet build`.
4. Kiểm thử các API/giao diện liên quan.
5. Commit theo dạng: `feat: them quy trinh kiem ke` hoặc `fix: chan xuat vuot ton`.
6. Push branch và tạo Pull Request vào `develop`.

## Phân công gợi ý

| Thành viên | Khu vực phụ trách |
|---|---|
| Thành viên 1 | Domain, EF Core, InventoryService, business rules |
| Thành viên 2 | Controllers, RBAC, API và kiểm thử endpoint |
| Thành viên 3 | `wwwroot/index.html`, UX responsive, tài liệu và kiểm thử giao diện |

Một người có thể review phần do người khác thực hiện. Không sửa trực tiếp code của branch thành viên khác nếu chưa trao đổi trong PR.

## Kiểm tra trước khi tạo PR

```powershell
dotnet restore
dotnet build
```

Nếu muốn reset dữ liệu local, dừng server rồi xóa `warehouse.db`, sau đó chạy lại project.