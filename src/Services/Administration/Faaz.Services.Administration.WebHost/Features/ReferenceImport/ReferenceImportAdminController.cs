using Faaz.Services.Administration.Domain;
using Faaz.Services.Administration.Domain.Entities;
using Faaz.Services.Administration.Infrastructure.ExcelImport;
using Faaz.Services.Administration.Infrastructure.Interfaces;
using Faaz.SharedKernel.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using static Faaz.Services.Administration.Domain.AdminEnums;

namespace Faaz.Services.Administration.WebHost.Features.ReferenceImport;

[Route("api/v1/admin/reference-import")]
[Authorize(Policy = "AdminOnly")]
public class ReferenceImportAdminController(
    IExcelImportExportService excel,
    IAdminActionLogServices auditLog) : FaazApiController
{
    private const long MaxFileBytes = 10 * 1024 * 1024; // 10MB

    [HttpGet("{entityKey}/template")]
    public IActionResult DownloadTemplate(string entityKey)
    {
        byte[] bytes;
        try
        {
            bytes = excel.GenerateTemplate(entityKey);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResponse.Fail(400, ex.Message));
        }

        return File(bytes,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"{entityKey}-import-template.xlsx");
    }

    [HttpPost("{entityKey}/upload")]
    [DisableRequestSizeLimit]
    public async Task<IActionResult> Upload(
        string entityKey,
        IFormFile file,
        [FromQuery] bool updateExisting = false,
        CancellationToken ct = default)
    {
        if (file.Length == 0) return BadRequest(ApiResponse.Fail(400, "File is empty."));
        if (file.Length > MaxFileBytes) return BadRequest(ApiResponse.Fail(400, "File exceeds the 10MB limit."));

        ImportSummary summary;
        try
        {
            await using var stream = file.OpenReadStream();
            summary = await excel.ImportAsync(entityKey, stream, updateExisting, ct);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResponse.Fail(400, ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse.Fail(400, ex.Message));
        }

        var adminId = GetUserId();
        var srNo    = await auditLog.NewSerialNumberAsync(ct);
        await auditLog.AddAsync(new AdminActionLog
        {
            SrNo        = srNo,
            AdminUserId = adminId,
            Action      = AdminAction.BulkImport,
            EntityType  = entityKey,
            EntityId    = Guid.Empty,
            Notes       = $"Imported {file.FileName}: {summary.Inserted} inserted, {summary.Updated} updated, {summary.Skipped} skipped, {summary.Failed} failed.",
            PerformedAt = DateTime.UtcNow
        }, ct);
        await auditLog.SaveChangesAsync(ct);

        return Ok(ApiResponse.Ok(summary));
    }
}
