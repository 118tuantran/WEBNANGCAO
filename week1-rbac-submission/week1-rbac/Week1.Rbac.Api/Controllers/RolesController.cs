using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Week1.Rbac.Api.Contracts;
using Week1.Rbac.Api.Data;
using Week1.Rbac.Api.Models;

namespace Week1.Rbac.Api.Controllers;

[ApiController]
[Route("api/roles")]
public sealed class RolesController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<RoleResponse>>> GetAll(CancellationToken cancellationToken)
    {
        var roles = await db.Roles
            .AsNoTracking()
            .Include(x => x.RolePermissions).ThenInclude(x => x.Permission)
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);

        return Ok(roles.Select(ToResponse).ToList());
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<RoleResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var role = await db.Roles
            .AsNoTracking()
            .Include(x => x.RolePermissions).ThenInclude(x => x.Permission)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        return role is null ? NotFound() : Ok(ToResponse(role));
    }

    [HttpPost]
    public async Task<ActionResult<RoleResponse>> Create(CreateRoleRequest request, CancellationToken cancellationToken)
    {
        var role = new Role
        {
            Name = request.Name.Trim(),
            Description = request.Description?.Trim()
        };

        db.Roles.Add(role);

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            return Conflict(new { message = "Role name already exists." });
        }

        return CreatedAtAction(nameof(GetById), new { id = role.Id }, ToResponse(role));
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, UpdateRoleRequest request, CancellationToken cancellationToken)
    {
        var role = await db.Roles.FindAsync([id], cancellationToken);
        if (role is null)
        {
            return NotFound();
        }

        role.Name = request.Name.Trim();
        role.Description = request.Description?.Trim();

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            return Conflict(new { message = "Role name already exists." });
        }

        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var role = await db.Roles.FindAsync([id], cancellationToken);
        if (role is null)
        {
            return NotFound();
        }

        db.Roles.Remove(role);
        await db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpPut("{roleId:guid}/permissions/{permissionId:guid}")]
    public async Task<IActionResult> AssignPermission(Guid roleId, Guid permissionId, CancellationToken cancellationToken)
    {
        var roleExists = await db.Roles.AnyAsync(x => x.Id == roleId, cancellationToken);
        var permissionExists = await db.Permissions.AnyAsync(x => x.Id == permissionId, cancellationToken);
        if (!roleExists || !permissionExists)
        {
            return NotFound();
        }

        if (!await db.RolePermissions.AnyAsync(x => x.RoleId == roleId && x.PermissionId == permissionId, cancellationToken))
        {
            db.RolePermissions.Add(new RolePermission { RoleId = roleId, PermissionId = permissionId });
            await db.SaveChangesAsync(cancellationToken);
        }

        return NoContent();
    }

    [HttpDelete("{roleId:guid}/permissions/{permissionId:guid}")]
    public async Task<IActionResult> RemovePermission(Guid roleId, Guid permissionId, CancellationToken cancellationToken)
    {
        var rolePermission = await db.RolePermissions.FindAsync([roleId, permissionId], cancellationToken);
        if (rolePermission is null)
        {
            return NotFound();
        }

        db.RolePermissions.Remove(rolePermission);
        await db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    private static RoleResponse ToResponse(Role role) =>
        new(role.Id, role.Name, role.Description, role.CreatedAt,
            role.RolePermissions.Select(x => x.Permission.Code).OrderBy(x => x).ToList());

    private static bool IsUniqueViolation(DbUpdateException ex) =>
        ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation };
}
