using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Week1.Rbac.Api.Data;
using Week1.Rbac.Api.Models;

namespace Week1.Rbac.Api.Controllers;

[ApiController]
[Route("api/user-roles")]
public sealed class UserRolesController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<object>>> GetAll(CancellationToken cancellationToken)
    {
        var records = await db.UserRoles
            .AsNoTracking()
            .Include(x => x.User)
            .Include(x => x.Role)
            .OrderBy(x => x.User.Email).ThenBy(x => x.Role.Name)
            .ToListAsync(cancellationToken);

        return Ok(records.Select(x => new { x.UserId, x.RoleId, UserEmail = x.User.Email, RoleName = x.Role.Name }).ToList());
    }

    [HttpGet("{userId:guid}/{roleId:guid}")]
    public async Task<ActionResult<object>> GetByIds(Guid userId, Guid roleId, CancellationToken cancellationToken)
    {
        var record = await db.UserRoles
            .AsNoTracking()
            .Include(x => x.User)
            .Include(x => x.Role)
            .FirstOrDefaultAsync(x => x.UserId == userId && x.RoleId == roleId, cancellationToken);

        return record is null ? NotFound() : Ok(new { record.UserId, record.RoleId, UserEmail = record.User.Email, RoleName = record.Role.Name });
    }
}
