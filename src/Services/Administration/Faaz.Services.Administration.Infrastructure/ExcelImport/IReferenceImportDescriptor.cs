namespace Faaz.Services.Administration.Infrastructure.ExcelImport;

// One descriptor per importable entity (University, Subject, Programme, Service). The generic
// ExcelImportExportService owns file I/O, template formatting, the row cap, and the transaction —
// a descriptor only knows its own columns and how to turn one row into an upsert against its entity.
public interface IReferenceImportDescriptor
{
    // "universities" | "subjects" | "programmes" | "services" — used in the route and filenames.
    string EntityKey { get; }
    string DisplayName { get; }
    IReadOnlyList<ImportColumn> Columns { get; }

    // One realistic example row, same order as Columns — written into the template directly below
    // the header, visually marked, so admins see the expected shape without guessing.
    IReadOnlyList<string?> ExampleRow { get; }

    // cellValues is aligned 1:1 with Columns. Mutates the DbContext's change tracker (Add/update an
    // existing tracked entity) but does not call SaveChangesAsync — the engine saves once per file.
    Task<ImportRowResult> ImportRowAsync(int rowNumber, IReadOnlyList<string?> cellValues, bool updateExisting, CancellationToken ct);
}
