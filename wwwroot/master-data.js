async function loadMasterData() {
  try {
    const [products, categories, warehouses, suppliers] = await Promise.all([
      api('/api/admin/products'),
      api('/api/admin/categories'),
      api('/api/admin/warehouses'),
      api('/api/admin/suppliers')
    ]);
    $('master').innerHTML = `<div class="page-head"><div><div class="eyebrow">Dữ liệu nền</div><h1>Danh mục kho</h1><p>Manager quản lý SKU, danh mục, kho và nhà cung cấp.</p></div></div><div class="workspace"><h2>Dữ liệu hiện có</h2><div class="table-wrap"><table class="data-table"><thead><tr><th>Loại</th><th>Mã hoặc tên</th><th>Số lượng</th></tr></thead><tbody><tr><td>SKU</td><td>${products.map(x => x.sku).join(', ')}</td><td>${products.length}</td></tr><tr><td>Danh mục</td><td>${categories.map(x => x.name).join(', ')}</td><td>${categories.length}</td></tr><tr><td>Kho</td><td>${warehouses.map(x => x.code).join(', ')}</td><td>${warehouses.length}</td></tr><tr><td>Nhà cung cấp</td><td>${suppliers.map(x => x.code).join(', ') || 'Chưa có'}</td><td>${suppliers.length}</td></tr></tbody></table></div></div>`;
  } catch (error) {
    $('master').innerHTML = `<div class="login-error">${error.message}</div>`;
  }
}

async function loadAdmin() {
  try {
    const users = await api('/api/admin/users');
    $('admin').innerHTML = `<div class="page-head"><div><div class="eyebrow">Thiết lập hệ thống</div><h1>Quản trị tài khoản</h1><p>Admin quản lý trạng thái và vai trò người dùng.</p></div></div><div class="workspace"><h2>Tài khoản hiện có</h2><div class="table-wrap"><table class="data-table"><thead><tr><th>Tên đăng nhập</th><th>Vai trò</th><th>Trạng thái</th></tr></thead><tbody>${users.map(user => `<tr><td>${user.username}</td><td>${user.role}</td><td>${user.isActive ? 'Đang hoạt động' : 'Đã khóa'}</td></tr>`).join('')}</tbody></table></div></div>`;
  } catch (error) {
    $('admin').innerHTML = `<div class="login-error">${error.message}</div>`;
  }
}
