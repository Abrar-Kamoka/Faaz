using Faaz.Services.Administration.Domain.Entities;
using Faaz.Services.Administration.Infrastructure.DatabaseContext;
using Microsoft.EntityFrameworkCore;

namespace Faaz.Services.Administration.Infrastructure.ExcelImport.Descriptors;

// Row: Name*, Description, Category, SortOrder
// Natural key: Name (case-insensitive).
internal sealed class ServiceImportDescriptor : IReferenceImportDescriptor
{
    private readonly AdminDbContext _db;
    public ServiceImportDescriptor(AdminDbContext db) { _db = db; }

    public string EntityKey => "services";
    public string DisplayName => "Services";

    public IReadOnlyList<ImportColumn> Columns { get; } =
    [
        new ImportColumn { Header = "Name", Required = true, Width = 35 },
        new ImportColumn { Header = "Description", Width = 40 },
        new ImportColumn { Header = "Category" },
        new ImportColumn { Header = "SortOrder", Width = 10 },
    ];

    public IReadOnlyList<string?> ExampleRow { get; } =
        ["Mock Interview Practice", "One-to-one mock interviews with feedback", "Application Support", "65"];

    public async Task<ImportRowResult> ImportRowAsync(int rowNumber, IReadOnlyList<string?> v, bool updateExisting, CancellationToken ct)
    {
        var name = v[0];
        if (string.IsNullOrWhiteSpace(name))
            return new ImportRowResult(rowNumber, ImportRowStatus.Failed, "Name is required.");

        var existing = await _db.Services.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Name.ToLower() == name.ToLower(), ct);

        if (existing is not null && !updateExisting)
            return new ImportRowResult(rowNumber, ImportRowStatus.Skipped, $"Already exists: '{existing.Name}'.");

        int? sortOrder = int.TryParse(v[3], out var so) ? so : null;

        var target = existing ?? new Service { SrNo = await NextSrNoAsync(ct) };
        target.Name        = name;
        target.Description = v[1];
        target.Category    = v[2];
        target.SortOrder    = sortOrder ?? target.SortOrder;
        target.IsActive     = true;

        if (existing is null)
            await _db.Services.AddAsync(target, ct);

        await _db.SaveChangesAsync(ct);

        return new ImportRowResult(rowNumber, existing is null ? ImportRowStatus.Inserted : ImportRowStatus.Updated, name);
    }

    private async Task<int> NextSrNoAsync(CancellationToken ct)
    {
        var max = await _db.Services.IgnoreQueryFilters().MaxAsync(x => (int?)x.SrNo, ct);
        return (max ?? 0) + 1;
    }
}
