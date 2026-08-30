namespace Faaz.Services.Administration.Infrastructure.ExcelImport;

public enum ImportRowStatus
{
    Inserted = 1,
    Updated  = 2,
    Skipped  = 3,
    Failed   = 4
}

public sealed record ImportRowResult(int RowNumber, ImportRowStatus Status, string Message);

public sealed record ImportSummary(
    int TotalRows,
    int Inserted,
    int Updated,
    int Skipped,
    int Failed,
    IReadOnlyList<ImportRowResult> Rows);
