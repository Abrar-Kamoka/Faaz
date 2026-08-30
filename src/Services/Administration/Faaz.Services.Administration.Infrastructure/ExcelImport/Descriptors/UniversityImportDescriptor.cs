using Faaz.Services.Administration.Domain.Entities;
using Faaz.Services.Administration.Infrastructure.DatabaseContext;
using Microsoft.EntityFrameworkCore;

namespace Faaz.Services.Administration.Infrastructure.ExcelImport.Descriptors;

// Row: Name*, Ukprn, Country, Nation, City, InstitutionType, WebsiteUrl, IsRussellGroup
// Natural key: Ukprn if present, else Name+Country (case-insensitive) — matches how a real HESA
// extract vs. a manually-curated row would each be keyed.
internal sealed class UniversityImportDescriptor : IReferenceImportDescriptor
{
    private readonly AdminDbContext _db;
    public UniversityImportDescriptor(AdminDbContext db) { _db = db; }

    public string EntityKey => "universities";
    public string DisplayName => "Universities";

    public IReadOnlyList<ImportColumn> Columns { get; } =
    [
        new ImportColumn { Header = "Name", Required = true },
        new ImportColumn { Header = "Ukprn", Width = 14 },
        new ImportColumn { Header = "Country" },
        new ImportColumn { Header = "Nation", DropdownOptions = ["England", "Scotland", "Wales", "NorthernIreland", "Overseas"] },
        new ImportColumn { Header = "City" },
        new ImportColumn { Header = "InstitutionType", DropdownOptions = ["University", "FE College", "Conservatoire", "Specialist"] },
        new ImportColumn { Header = "WebsiteUrl", Width = 35 },
        new ImportColumn { Header = "IsRussellGroup", DropdownOptions = ["TRUE", "FALSE"], Width = 14 },
    ];

    public IReadOnlyList<string?> ExampleRow { get; } =
        ["University of Manchester", "10007784", "United Kingdom", "England", "Manchester", "University", "https://www.manchester.ac.uk", "TRUE"];

    public async Task<ImportRowResult> ImportRowAsync(int rowNumber, IReadOnlyList<string?> v, bool updateExisting, CancellationToken ct)
    {
        var name = v[0];
        if (string.IsNullOrWhiteSpace(name))
            return new ImportRowResult(rowNumber, ImportRowStatus.Failed, "Name is required.");

        var ukprn = v[1];
        var country = v[2];

        var existing = !string.IsNullOrWhiteSpace(ukprn)
            ? await _db.Universities.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Ukprn == ukprn, ct)
            : await _db.Universities.IgnoreQueryFilters()
                       .FirstOrDefaultAsync(x => x.Name.ToLower() == name.ToLower() && x.Country == country, ct);

        if (existing is not null && !updateExisting)
            return new ImportRowResult(rowNumber, ImportRowStatus.Skipped, $"Already exists: '{existing.Name}'.");

        var target = existing ?? new University { SrNo = await NextSrNoAsync(ct) };
        target.Name            = name;
        target.Ukprn            = ukprn;
        target.Country          = country;
        target.Nation            = v[3];
        target.City              = v[4];
        target.InstitutionType   = v[5];
        target.WebsiteUrl        = v[6];
        target.IsRussellGroup    = string.Equals(v[7], "TRUE", StringComparison.OrdinalIgnoreCase);
        target.IsActive          = true;
        target.DataSource        = "Admin Excel Import";
        target.LastVerifiedAt    = DateTime.UtcNow;

        if (existing is null)
            await _db.Universities.AddAsync(target, ct);

        await _db.SaveChangesAsync(ct);

        return new ImportRowResult(rowNumber, existing is null ? ImportRowStatus.Inserted : ImportRowStatus.Updated, name);
    }

    private async Task<int> NextSrNoAsync(CancellationToken ct)
    {
        var max = await _db.Universities.IgnoreQueryFilters().MaxAsync(x => (int?)x.SrNo, ct);
        return (max ?? 0) + 1;
    }
}
