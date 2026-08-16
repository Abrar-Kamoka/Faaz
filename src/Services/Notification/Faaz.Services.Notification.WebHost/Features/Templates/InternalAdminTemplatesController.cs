using Faaz.Services.Notification.Domain.Entities;
using Faaz.Services.Notification.Infrastructure.DatabaseContext;
using Faaz.SharedKernel.Results;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using static Faaz.Services.Notification.Domain.NotificationEnums;

namespace Faaz.Services.Notification.WebHost.Features.Templates;

[Route("internal/admin/templates")]
[Tags("Internal - Admin")]
[IgnoreAntiforgeryToken]
public class InternalAdminTemplatesController : ControllerBase
{
    private readonly NotificationDbContext _db;
    private readonly IConfiguration _config;

    public InternalAdminTemplatesController(NotificationDbContext db, IConfiguration config)
    {
        _db     = db;
        _config = config;
    }

    [HttpGet]
    public async Task<IActionResult> GetTemplates(CancellationToken ct = default)
    {
        if (!IsInternal()) return StatusCode(403, ApiResponse.Fail(403, "Forbidden."));

        var items = await _db.NotificationTemplates
            .OrderBy(t => t.Key)
            .Select(t => new
            {
                t.Id, t.Key, Channel = t.Channel.ToString(), t.Subject, t.Body, t.Description, t.UpdatedAt
            })
            .ToListAsync(ct);

        return Ok(ApiResponse.Ok(items));
    }

    [HttpPut("{templateId:guid}")]
    public async Task<IActionResult> UpdateTemplate(Guid templateId, [FromBody] UpdateTemplateBody req, CancellationToken ct = default)
    {
        if (!IsInternal()) return StatusCode(403, ApiResponse.Fail(403, "Forbidden."));

        var template = await _db.NotificationTemplates.FirstOrDefaultAsync(t => t.Id == templateId, ct);
        if (template is null) return NotFound(ApiResponse.Fail(404, "Template not found."));

        template.Subject   = req.Subject;
        template.Body      = req.Body;
        template.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        return Ok(ApiResponse.NoContent("Template updated."));
    }

    private bool IsInternal()
    {
        var key = HttpContext.Request.Headers["X-Service-Key"].FirstOrDefault();
        return !string.IsNullOrWhiteSpace(key) && key == _config["ServiceApiKey"];
    }
}

public record UpdateTemplateBody(string Subject, string Body);
