using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Week1.Rbac.Api.Contracts;
using Week1.Rbac.Api.Data;
using Week1.Rbac.Api.Models;

namespace Week1.Rbac.Api.Controllers;

[ApiController]
[Route("api/permissions")]
public sealed class PermissionsController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<PermissionResponse>>> GetAll(CancellationToken cancellationToken)
    {
        var permissions = await db.Permissions
            .AsNoTracking()
            .OrderBy(x => x.Code)
            .ToListAsync(cancellationToken);

        return Ok(permissions.Select(ToResponse).ToList());
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<PermissionResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var permission = await db.Permissions
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        return permission is null ? NotFound() : Ok(ToResponse(permission));
    }

    [HttpPost]
    public async Task<ActionResult<PermissionResponse>> Create(CreatePermissionRequest request, CancellationToken cancellationToken)
    {
        var permission = new Permission
        {
            Code = request.Code.Trim(),
            Description = request.Description?.Trim()
        };

        db.Permissions.Add(permission);

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            return Conflict(new { message = "Permission code already exists." });
        }

        return CreatedAtAction(nameof(GetById), new { id = permission.Id }, ToResponse(permission));
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, UpdatePermissionRequest request, CancellationToken cancellationToken)
    {
        var permission = await db.Permissions.FindAsync([id], cancellationToken);
        if (permission is null)
        {
            return NotFound();
        }

        permission.Code = request.Code.Trim();
        permission.Description = request.Description?.Trim();

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            return Conflict(new { message = "Permission code already exists." });
        }

        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var permission = await db.Permissions.FindAsync([id], cancellationToken);
        if (permission is null)
        {
            return NotFound();
        }

        db.Permissions.Remove(permission);
        await db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    private static PermissionResponse ToResponse(Permission permission) =>
        new(permission.Id, permission.Code, permission.Description, permission.CreatedAt);

    private static bool IsUniqueViolation(DbUpdateException ex) =>
        ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation };
}
