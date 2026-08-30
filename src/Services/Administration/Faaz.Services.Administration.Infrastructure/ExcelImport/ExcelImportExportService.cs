using ClosedXML.Excel;

namespace Faaz.Services.Administration.Infrastructure.ExcelImport;

public interface IExcelImportExportService
{
    byte[] GenerateTemplate(string entityKey);
    Task<ImportSummary> ImportAsync(string entityKey, Stream file, bool updateExisting, CancellationToken ct = default);
}

internal sealed class ExcelImportExportService : IExcelImportExportService
{
    // Keeps a single upload synchronous (no Hangfire job needed) — large datasets get split by
    // the admin into multiple uploads rather than this service silently running for minutes.
    private const int MaxRows = 5000;

    private readonly Dictionary<string, IReferenceImportDescriptor> _descriptors;

    public ExcelImportExportService(IEnumerable<IReferenceImportDescriptor> descriptors)
    {
        _descriptors = descriptors.ToDictionary(d => d.EntityKey, StringComparer.OrdinalIgnoreCase);
    }

    private IReferenceImportDescriptor Resolve(string entityKey) =>
        _descriptors.TryGetValue(entityKey, out var d)
            ? d
            : throw new ArgumentException($"Unknown reference entity key '{entityKey}'. Valid keys: {string.Join(", ", _descriptors.Keys)}");

    public byte[] GenerateTemplate(string entityKey)
    {
        var descriptor = Resolve(entityKey);

        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add(descriptor.DisplayName);

        for (var i = 0; i < descriptor.Columns.Count; i++)
        {
            var col = descriptor.Columns[i];
            var cell = ws.Cell(1, i + 1);
            cell.Value = col.Header + (col.Required ? " *" : "");
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#DCE6F1");
            ws.Column(i + 1).Width = col.Width;

            if (col.DropdownOptions is { Length: > 0 })
            {
                // Validate a generous range below the header, not just the example row, so the
                // dropdown keeps working as the admin adds real rows underneath it.
                var validationRange = ws.Range(2, i + 1, 1000, i + 1);
                validationRange.CreateDataValidation().List(string.Join(",", col.DropdownOptions), true);
            }
        }

        for (var i = 0; i < descriptor.ExampleRow.Count; i++)
        {
            var cell = ws.Cell(2, i + 1);
            cell.Value = descriptor.ExampleRow[i];
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#F2F2F2");
            cell.Style.Font.Italic = true;
        }
        if (descriptor.ExampleRow.Count > 0)
        {
            ws.Cell(2, descriptor.Columns.Count + 2).Value = "<- EXAMPLE ROW — delete or overwrite before uploading";
            ws.Cell(2, descriptor.Columns.Count + 2).Style.Font.FontColor = XLColor.FromHtml("#C00000");
        }

        ws.SheetView.FreezeRows(1);
        ws.Row(1).Height = 20;

        using var stream = new MemoryStream();
        wb.SaveAs(stream);
        return stream.ToArray();
    }

    public async Task<ImportSummary> ImportAsync(string entityKey, Stream file, bool updateExisting, CancellationToken ct = default)
    {
        var descriptor = Resolve(entityKey);

        using var wb = new XLWorkbook(file);
        var ws = wb.Worksheets.First();
        var lastRow = ws.LastRowUsed()?.RowNumber() ?? 1;
        var dataRowCount = Math.Max(0, lastRow - 1); // minus header

        if (dataRowCount > MaxRows)
            throw new InvalidOperationException(
                $"This file has {dataRowCount} rows, which exceeds the {MaxRows}-row limit per upload. Split it into smaller files and upload them one at a time.");

        var results = new List<ImportRowResult>();

        for (var row = 2; row <= lastRow; row++)
        {
            var isExampleRow = row == 2 && RowMatches(ws, row, descriptor.ExampleRow);
            var cellValues = descriptor.Columns
                .Select((_, i) => ws.Cell(row, i + 1).GetString())
                .Select(v => string.IsNullOrWhiteSpace(v) ? null : v.Trim())
                .ToList();

            if (isExampleRow || cellValues.All(v => v is null))
            {
                results.Add(new ImportRowResult(row, ImportRowStatus.Skipped, isExampleRow ? "Example row — ignored." : "Empty row."));
                continue;
            }

            var result = await descriptor.ImportRowAsync(row, cellValues, updateExisting, ct);
            results.Add(result);
        }

        return new ImportSummary(
            TotalRows: results.Count,
            Inserted: results.Count(r => r.Status == ImportRowStatus.Inserted),
            Updated:  results.Count(r => r.Status == ImportRowStatus.Updated),
            Skipped:  results.Count(r => r.Status == ImportRowStatus.Skipped),
            Failed:   results.Count(r => r.Status == ImportRowStatus.Failed),
            Rows: results);
    }

    private static bool RowMatches(IXLWorksheet ws, int row, IReadOnlyList<string?> exampleRow)
    {
        if (exampleRow.Count == 0) return false;
        for (var i = 0; i < exampleRow.Count; i++)
        {
            var actual = ws.Cell(row, i + 1).GetString();
            if (!string.Equals(actual?.Trim(), exampleRow[i]?.Trim(), StringComparison.OrdinalIgnoreCase))
                return false;
        }
        return true;
    }
}
