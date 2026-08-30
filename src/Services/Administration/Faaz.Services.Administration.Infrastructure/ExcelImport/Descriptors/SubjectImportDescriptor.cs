using Faaz.Services.Administration.Domain.Entities;
using Faaz.Services.Administration.Infrastructure.DatabaseContext;
using Microsoft.EntityFrameworkCore;

namespace Faaz.Services.Administration.Infrastructure.ExcelImport.Descriptors;

// Row: Name*, HecosCode, Category
// Natural key: HecosCode if present, else Name (case-insensitive).
internal sealed class SubjectImportDescriptor : IReferenceImportDescriptor
{
    private readonly AdminDbContext _db;
    public SubjectImportDescriptor(AdminDbContext db) { _db = db; }

    public string EntityKey => "subjects";
    public string DisplayName => "Subjects";

    public IReadOnlyList<ImportColumn> Columns { get; } =
    [
        new ImportColumn { Header = "Name", Required = true },
        new ImportColumn { Header = "HecosCode", Width = 14 },
        new ImportColumn { Header = "Category (CAH group)", Width = 28 },
    ];

    public IReadOnlyList<string?> ExampleRow { get; } = ["Computer Science", "100366", "Computing"];

    public async Task<ImportRowResult> ImportRowAsync(int rowNumber, IReadOnlyList<string?> v, bool updateExisting, CancellationToken ct)
    {
        var name = v[0];
        if (string.IsNullOrWhiteSpace(name))
            return new ImportRowResult(rowNumber, ImportRowStatus.Failed, "Name is required.");

        var hecosCode = v[1];

        var existing = !string.IsNullOrWhiteSpace(hecosCode)
            ? await _db.Subjects.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.HecosCode == hecosCode, ct)
            : await _db.Subjects.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Name.ToLower() == name.ToLower(), ct);

        if (existing is not null && !updateExisting)
            return new ImportRowResult(rowNumber, ImportRowStatus.Skipped, $"Already exists: '{existing.Name}'.");

        var target = existing ?? new Subject { SrNo = await NextSrNoAsync(ct) };
        target.Name           = name;
        target.HecosCode       = hecosCode;
        target.Category        = v[2];
        target.IsActive        = true;
        target.DataSource      = "Admin Excel Import";
        target.LastVerifiedAt  = DateTime.UtcNow;

        if (existing is null)
            await _db.Subjects.AddAsync(target, ct);

        await _db.SaveChangesAsync(ct);

        return new ImportRowResult(rowNumber, existing is null ? ImportRowStatus.Inserted : ImportRowStatus.Updated, name);
    }

    private async Task<int> NextSrNoAsync(CancellationToken ct)
    {
        var max = await _db.Subjects.IgnoreQueryFilters().MaxAsync(x => (int?)x.SrNo, ct);
        return (max ?? 0) + 1;
    }
}
