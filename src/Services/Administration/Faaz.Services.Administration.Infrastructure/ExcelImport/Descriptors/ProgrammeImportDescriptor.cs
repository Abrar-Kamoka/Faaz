using Faaz.Services.Administration.Domain.Entities;
using Faaz.Services.Administration.Infrastructure.DatabaseContext;
using Microsoft.EntityFrameworkCore;
using static Faaz.SharedKernel.SharedEnums;

namespace Faaz.Services.Administration.Infrastructure.ExcelImport.Descriptors;

// Row: UniversityName*, Title*, StudyLevel*, Mode*, DurationMonths, UcasCode, EntryRequirements,
//      TuitionFeeDomesticGbp, TuitionFeeInternationalGbp, SubjectNames (comma-separated)
// Natural key: UniversityId + Title + StudyLevel + Mode. References the university and subjects
// by name (not Guid) since that's what an admin/ops person filling this in by hand actually has.
internal sealed class ProgrammeImportDescriptor : IReferenceImportDescriptor
{
    private readonly AdminDbContext _db;
    public ProgrammeImportDescriptor(AdminDbContext db) { _db = db; }

    public string EntityKey => "programmes";
    public string DisplayName => "Programmes";

    public IReadOnlyList<ImportColumn> Columns { get; } =
    [
        new ImportColumn { Header = "UniversityName", Required = true, Width = 30 },
        new ImportColumn { Header = "Title", Required = true, Width = 40 },
        new ImportColumn { Header = "StudyLevel", Required = true, DropdownOptions = Enum.GetNames<StudyLevel>() },
        new ImportColumn { Header = "Mode", Required = true, DropdownOptions = Enum.GetNames<ProgrammeMode>() },
        new ImportColumn { Header = "DurationMonths", Width = 14 },
        new ImportColumn { Header = "UcasCode", Width = 12 },
        new ImportColumn { Header = "EntryRequirements", Width = 40 },
        new ImportColumn { Header = "TuitionFeeDomesticGbp", Width = 18 },
        new ImportColumn { Header = "TuitionFeeInternationalGbp", Width = 20 },
        new ImportColumn { Header = "SubjectNames (comma-separated)", Width = 30 },
    ];

    public IReadOnlyList<string?> ExampleRow { get; } =
    [
        "University of Manchester", "BSc (Hons) Computer Science", "Undergraduate", "FullTime",
        "36", "G400", "AAA at A-Level including Mathematics", "9250", "26000", "Computer Science"
    ];

    public async Task<ImportRowResult> ImportRowAsync(int rowNumber, IReadOnlyList<string?> v, bool updateExisting, CancellationToken ct)
    {
        var universityName = v[0];
        var title           = v[1];

        if (string.IsNullOrWhiteSpace(universityName) || string.IsNullOrWhiteSpace(title))
            return new ImportRowResult(rowNumber, ImportRowStatus.Failed, "UniversityName and Title are required.");

        if (!Enum.TryParse<StudyLevel>(v[2], ignoreCase: true, out var studyLevel))
            return new ImportRowResult(rowNumber, ImportRowStatus.Failed, $"Invalid StudyLevel '{v[2]}'. Expected one of: {string.Join(", ", Enum.GetNames<StudyLevel>())}.");

        if (!Enum.TryParse<ProgrammeMode>(v[3], ignoreCase: true, out var mode))
            return new ImportRowResult(rowNumber, ImportRowStatus.Failed, $"Invalid Mode '{v[3]}'. Expected one of: {string.Join(", ", Enum.GetNames<ProgrammeMode>())}.");

        var university = await _db.Universities.IgnoreQueryFilters()
                                   .FirstOrDefaultAsync(x => x.Name.ToLower() == universityName.ToLower(), ct);
        if (university is null)
            return new ImportRowResult(rowNumber, ImportRowStatus.Failed, $"University '{universityName}' not found — add it first (universities import).");

        var existing = await _db.Programmes.IgnoreQueryFilters()
                                 .FirstOrDefaultAsync(x => x.UniversityId == university.Id
                                                         && x.Title.ToLower() == title.ToLower()
                                                         && x.StudyLevel == studyLevel
                                                         && x.Mode == mode, ct);

        if (existing is not null && !updateExisting)
            return new ImportRowResult(rowNumber, ImportRowStatus.Skipped, $"Already exists: '{title}' at '{university.Name}'.");

        var subjectNames = (v[9] ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var subjectIds = new List<Guid>();
        foreach (var subjectName in subjectNames)
        {
            var subject = await _db.Subjects.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Name.ToLower() == subjectName.ToLower(), ct);
            if (subject is null)
                return new ImportRowResult(rowNumber, ImportRowStatus.Failed, $"Subject '{subjectName}' not found — add it first (subjects import).");
            subjectIds.Add(subject.Id);
        }

        var target = existing;
        var isNew = target is null;
        if (isNew)
        {
            target = new Programme { SrNo = await NextSrNoAsync(ct), UniversityId = university.Id };
            await _db.Programmes.AddAsync(target!, ct);
        }

        target!.Title                        = title;
        target.StudyLevel                    = studyLevel;
        target.Mode                          = mode;
        target.DurationMonths                = int.TryParse(v[4], out var dm) ? dm : null;
        target.UcasCode                      = v[5];
        target.EntryRequirements             = v[6];
        target.TuitionFeeDomesticGbp         = decimal.TryParse(v[7], out var fd) ? fd : null;
        target.TuitionFeeInternationalGbp    = decimal.TryParse(v[8], out var fi) ? fi : null;
        target.IsActive                      = true;
        target.DataSource                    = "Admin Excel Import";
        target.LastVerifiedAt                = DateTime.UtcNow;

        if (!isNew)
        {
            // Reconcile the subject links to whatever this row now lists.
            var current = await _db.Set<ProgrammeSubject>().Where(x => x.ProgrammeId == target.Id).ToListAsync(ct);
            _db.RemoveRange(current.Where(x => !subjectIds.Contains(x.SubjectId)));
            foreach (var toAdd in subjectIds.Where(id => current.All(c => c.SubjectId != id)))
                await _db.Set<ProgrammeSubject>().AddAsync(new ProgrammeSubject { ProgrammeId = target.Id, SubjectId = toAdd }, ct);
        }
        else
        {
            foreach (var subjectId in subjectIds)
                await _db.Set<ProgrammeSubject>().AddAsync(new ProgrammeSubject { ProgrammeId = target.Id, SubjectId = subjectId }, ct);
        }

        await _db.SaveChangesAsync(ct);

        return new ImportRowResult(rowNumber, isNew ? ImportRowStatus.Inserted : ImportRowStatus.Updated, title);
    }

    private async Task<int> NextSrNoAsync(CancellationToken ct)
    {
        var max = await _db.Programmes.IgnoreQueryFilters().MaxAsync(x => (int?)x.SrNo, ct);
        return (max ?? 0) + 1;
    }
}
