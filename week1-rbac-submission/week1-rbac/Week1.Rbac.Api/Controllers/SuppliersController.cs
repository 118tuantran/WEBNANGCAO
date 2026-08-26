using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Week1.Rbac.Api.Data;
using Week1.Rbac.Api.Models;

namespace Week1.Rbac.Api.Controllers;

[ApiController]
[Route("api/role-permissions")]
public sealed class RolePermissionsController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<object>>> GetAll(CancellationToken cancellationToken)
    {
        var records = await db.RolePermissions
            .AsNoTracking()
            .Include(x => x.Role)
            .Include(x => x.Permission)
            .OrderBy(x => x.Role.Name).ThenBy(x => x.Permission.Code)
            .ToListAsync(cancellationToken);

        return Ok(records.Select(x => new { x.RoleId, x.PermissionId, RoleName = x.Role.Name, PermissionCode = x.Permission.Code }).ToList());
    }

    [HttpGet("{roleId:guid}/{permissionId:guid}")]
    public async Task<ActionResult<object>> GetByIds(Guid roleId, Guid permissionId, CancellationToken cancellationToken)
    {
        var record = await db.RolePermissions
            .AsNoTracking()
            .Include(x => x.Role)
            .Include(x => x.Permission)
            .FirstOrDefaultAsync(x => x.RoleId == roleId && x.PermissionId == permissionId, cancellationToken);

        return record is null ? NotFound() : Ok(new { record.RoleId, record.PermissionId, RoleName = record.Role.Name, PermissionCode = record.Permission.Code });
    }
}
