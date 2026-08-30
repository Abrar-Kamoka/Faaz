using System.Diagnostics;
using System.Globalization;
using ClosedXML.Excel;
using CsvHelper;
using Faaz.Services.Administration.Domain.Entities;
using Faaz.Services.Administration.Infrastructure.DatabaseContext;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using static Faaz.SharedKernel.SharedEnums;

namespace Faaz.Services.Administration.Infrastructure.SeedData;

// Loads the real HESA Discover Uni dataset (CC BY 4.0, bundled under SeedData/DiscoverUni/ — see the
// README there) into Universities/Subjects/Programmes. This is the one-time bulk seed, deliberately
// NOT routed through the admin Excel importer — that tool is built for small hand-curated batches
// (5,000-row cap, one row reviewed at a time); this is ~31,000 real course rows in one shot.
//
// Self-healing: runs at startup and re-fills the catalog whenever it looks under-populated (below
// MinUniversityRowsBeforeReseed), so a fresh/wiped database — dev, staging, prod, doesn't matter —
// gets real data back without anyone re-running a manual step. Every insert is upserted against a
// natural key (Ukprn / HecosCode / University+Title+StudyLevel+Mode), so re-running this a hundred
// times in a row never duplicates a row — it just tops up whatever's missing.
//
// Undergraduate only — Discover Uni doesn't cover postgraduate. See README.md in the data folder.
public static class DiscoverUniSeeder
{
    // Below this, we treat the catalog as "not really populated" and re-run the seed — high enough
    // to catch a wipe/fresh-DB, low enough that normal admin curation (adding/removing a handful of
    // rows) never accidentally re-triggers a 31k-row parse on every boot.
    private const int MinUniversityRowsBeforeReseed = 100;

    private const string DataSourceLabel = "HESA Discover Uni 2025/26 (CC BY 4.0)";
    private const string DataSourceUrl = "https://www.hesa.ac.uk/support/tools-and-downloads/unistats";

    private static readonly Dictionary<string, string> NationByCountryCode = new(StringComparer.OrdinalIgnoreCase)
    {
        ["XF"] = "England",
        ["XG"] = "Northern Ireland",
        ["XH"] = "Scotland",
        ["XI"] = "Wales",
    };

    public static async Task SeedAsync(IServiceProvider sp)
    {
        using var scope = sp.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AdminDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<AdminDbContext>>();

        var universityCount = await db.Universities.IgnoreQueryFilters().CountAsync();
        if (universityCount >= MinUniversityRowsBeforeReseed)
        {
            logger.LogInformation("DiscoverUniSeeder: {Count} universities already present — skipping.", universityCount);
            return;
        }

        var dataDir = Path.Combine(AppContext.BaseDirectory, "SeedData", "DiscoverUni");
        if (!Directory.Exists(dataDir))
        {
            logger.LogWarning("DiscoverUniSeeder: bundled data folder not found at {Path} — skipping seed.", dataDir);
            return;
        }

        logger.LogInformation("DiscoverUniSeeder: catalog under-populated ({Count} universities) — seeding from bundled HESA data.", universityCount);
        var sw = Stopwatch.StartNew();

        db.ChangeTracker.AutoDetectChangesEnabled = false;
        try
        {
            var kisaimLabels = LoadKisaimLabels(Path.Combine(dataDir, "KISAIM.csv"));
            var subjectLookup = LoadSubjectLookup(Path.Combine(dataDir, "HECoS_CAH_Version_1.3.4.xlsx"));

            var subjectIdByCode = await SeedSubjectsAsync(db, subjectLookup);
            var universityIdByPubUkprn = await SeedUniversitiesAsync(db, Path.Combine(dataDir, "INSTITUTION.csv"));

            var courseSubjects = LoadCourseSubjects(Path.Combine(dataDir, "SBJ.csv"));
            var courseUcasCodes = LoadCourseUcasCodes(Path.Combine(dataDir, "UCASCOURSEID.csv"));

            await SeedProgrammesAsync(
                db, Path.Combine(dataDir, "KISCOURSE.csv"),
                universityIdByPubUkprn, subjectIdByCode, kisaimLabels, courseSubjects, courseUcasCodes, logger);
        }
        finally
        {
            db.ChangeTracker.AutoDetectChangesEnabled = true;
        }

        sw.Stop();
        logger.LogInformation("DiscoverUniSeeder: done in {Elapsed}.", sw.Elapsed);
    }

    private static string CourseKey(string pubukprn, string kisCourseId, string kisMode)
        => $"{pubukprn.Trim()}|{kisCourseId.Trim()}|{kisMode.Trim()}";

    private static string? NormalizeUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url)) return null;
        url = url.Trim();
        if (url.Length > 500) url = url[..500];
        return url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || url.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
            ? url
            : $"https://{url}";
    }

    private static string ToTitleCase(string text) =>
        CultureInfo.InvariantCulture.TextInfo.ToTitleCase(text.Trim().ToLowerInvariant());

    // "(CAH01-01-01) medical sciences (non-specific)" -> "medical sciences (non-specific)"
    private static string StripCodePrefix(string text)
    {
        var idx = text.IndexOf(')');
        return idx >= 0 && idx + 1 < text.Length ? text[(idx + 1)..].Trim() : text.Trim();
    }

    // ── Lookups ──────────────────────────────────────────────────────────────

    private static Dictionary<string, string> LoadKisaimLabels(string path)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        using var reader = new StreamReader(path);
        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
        csv.Read(); // header
        while (csv.Read())
        {
            var r = csv.Parser.Record;
            if (r is null || r.Length < 2) continue;
            result[r[0].Trim()] = r[1].Trim();
        }
        return result;
    }

    // CAH3 code -> (Name, Category). Sourced from the "CAH (V1.3.4)" sheet of the bundled HECoS_CAH
    // workbook: col A = CAH1 text, col C = CAH3 text, col F = CAH3 code-only.
    private static Dictionary<string, (string Name, string Category)> LoadSubjectLookup(string path)
    {
        var result = new Dictionary<string, (string, string)>(StringComparer.OrdinalIgnoreCase);
        using var wb = new XLWorkbook(path);
        var ws = wb.Worksheet("CAH (V1.3.4)");
        var lastRow = ws.LastRowUsed()!.RowNumber();
        for (var row = 2; row <= lastRow; row++)
        {
            var cah3Code = ws.Cell(row, 6).GetString().Trim();
            if (string.IsNullOrWhiteSpace(cah3Code)) continue;

            var cah1Text = ws.Cell(row, 1).GetString();
            var cah3Text = ws.Cell(row, 3).GetString();
            result[cah3Code] = (StripCodePrefix(cah3Text), StripCodePrefix(cah1Text));
        }
        return result;
    }

    private static Dictionary<string, List<string>> LoadCourseSubjects(string path)
    {
        var result = new Dictionary<string, List<string>>();
        using var reader = new StreamReader(path);
        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
        csv.Read();
        while (csv.Read())
        {
            var r = csv.Parser.Record; // PUBUKPRN,UKPRN,KISCOURSEID,KISMODE,SBJ
            if (r is null || r.Length < 5) continue;
            var code = r[4].Trim();
            if (string.IsNullOrWhiteSpace(code)) continue;

            var key = CourseKey(r[0], r[2], r[3]);
            if (!result.TryGetValue(key, out var list))
                result[key] = list = [];
            list.Add(code);
        }
        return result;
    }

    private static Dictionary<string, string> LoadCourseUcasCodes(string path)
    {
        var result = new Dictionary<string, string>();
        using var reader = new StreamReader(path);
        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
        csv.Read();
        while (csv.Read())
        {
            var r = csv.Parser.Record; // PUBUKPRN,UKPRN,KISCOURSEID,KISMODE,LOCID,UCASCOURSEID
            if (r is null || r.Length < 6) continue;
            var ucasCode = r[5].Trim();
            if (string.IsNullOrWhiteSpace(ucasCode)) continue;
            result.TryAdd(CourseKey(r[0], r[2], r[3]), ucasCode); // first location's code wins
        }
        return result;
    }

    // ── Universities ─────────────────────────────────────────────────────────

    private static async Task<Dictionary<string, Guid>> SeedUniversitiesAsync(AdminDbContext db, string csvPath)
    {
        var existing = await db.Universities.IgnoreQueryFilters()
            .Where(x => x.Ukprn != null)
            .Select(x => new { x.Ukprn, x.Id })
            .ToDictionaryAsync(x => x.Ukprn!, x => x.Id, StringComparer.OrdinalIgnoreCase);

        var nextSrNo = (await db.Universities.IgnoreQueryFilters().MaxAsync(x => (int?)x.SrNo) ?? 0) + 1;
        var toAdd = new List<University>();
        var seenInThisFile = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        using var reader = new StreamReader(csvPath);
        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
        csv.Read();
        while (csv.Read())
        {
            var r = csv.Parser.Record; // LEGAL_NAME,FIRST_TRADING_NAME,...,PROVURL,PUBUKPRN,UKPRN,COUNTRY,...
            if (r is null || r.Length < 9) continue;

            var pubUkprn = r[6].Trim();
            if (string.IsNullOrWhiteSpace(pubUkprn)) continue;
            // A PUBUKPRN can appear on several INSTITUTION rows in rare data-quality cases — first wins.
            if (!seenInThisFile.Add(pubUkprn)) continue;
            if (existing.ContainsKey(pubUkprn)) continue;

            var name = r[0].Trim();
            if (string.IsNullOrWhiteSpace(name)) name = r[1].Trim();
            if (string.IsNullOrWhiteSpace(name)) continue;
            if (name.Length > 200) name = name[..200];

            var entity = new University
            {
                SrNo           = nextSrNo++,
                Name           = name,
                Country        = "United Kingdom",
                Nation         = NationByCountryCode.GetValueOrDefault(r[8].Trim()),
                Ukprn          = pubUkprn,
                WebsiteUrl     = NormalizeUrl(r[5]),
                IsActive       = true,
                DataSource     = DataSourceLabel,
                SourceUrl      = DataSourceUrl,
                LastVerifiedAt = DateTime.UtcNow
            };
            toAdd.Add(entity);
            existing[pubUkprn] = entity.Id;
        }

        if (toAdd.Count > 0)
        {
            await db.Universities.AddRangeAsync(toAdd);
            await db.SaveChangesAsync();
        }
        return existing;
    }

    // ── Subjects ─────────────────────────────────────────────────────────────

    private static async Task<Dictionary<string, Guid>> SeedSubjectsAsync(
        AdminDbContext db, Dictionary<string, (string Name, string Category)> lookup)
    {
        var existing = await db.Subjects.IgnoreQueryFilters()
            .Where(x => x.HecosCode != null)
            .Select(x => new { x.HecosCode, x.Id })
            .ToDictionaryAsync(x => x.HecosCode!, x => x.Id, StringComparer.OrdinalIgnoreCase);

        var nextSrNo = (await db.Subjects.IgnoreQueryFilters().MaxAsync(x => (int?)x.SrNo) ?? 0) + 1;
        var toAdd = new List<Subject>();

        foreach (var (code, (name, category)) in lookup)
        {
            if (existing.ContainsKey(code)) continue;

            var entity = new Subject
            {
                SrNo           = nextSrNo++,
                Name           = ToTitleCase(name),
                Category       = ToTitleCase(category),
                HecosCode      = code,
                IsActive       = true,
                DataSource     = DataSourceLabel,
                SourceUrl      = DataSourceUrl,
                LastVerifiedAt = DateTime.UtcNow
            };
            toAdd.Add(entity);
            existing[code] = entity.Id;
        }

        if (toAdd.Count > 0)
        {
            await db.Subjects.AddRangeAsync(toAdd);
            await db.SaveChangesAsync();
        }
        return existing;
    }

    // ── Programmes (the ~31k-row one) ───────────────────────────────────────

    private static async Task SeedProgrammesAsync(
        AdminDbContext db, string csvPath,
        Dictionary<string, Guid> universityIdByPubUkprn,
        Dictionary<string, Guid> subjectIdByCode,
        Dictionary<string, string> kisaimLabels,
        Dictionary<string, List<string>> courseSubjects,
        Dictionary<string, string> courseUcasCodes,
        ILogger logger)
    {
        // Natural key mirrors ProgrammeImportDescriptor's: University + Title + StudyLevel + Mode.
        // Loading it once up front means every one of the 31k rows is an in-memory HashSet check
        // instead of a per-row database round trip.
        var existingKeys = (await db.Programmes.IgnoreQueryFilters()
                .Select(x => new { x.UniversityId, Title = x.Title.ToLower(), x.StudyLevel, x.Mode })
                .ToListAsync())
            .Select(x => (x.UniversityId, x.Title, x.StudyLevel, x.Mode))
            .ToHashSet();

        var nextSrNo = (await db.Programmes.IgnoreQueryFilters().MaxAsync(x => (int?)x.SrNo) ?? 0) + 1;

        const int batchSize = 1000;
        var batch = new List<Programme>(batchSize);
        var subjectLinks = new List<ProgrammeSubject>();
        var inserted = 0;
        var skippedNoUniversity = 0;

        using var reader = new StreamReader(csvPath);
        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
        csv.Read();
        while (csv.Read())
        {
            var r = csv.Parser.Record;
            if (r is null || r.Length < 35) continue;

            var pubUkprn    = r[0].Trim();
            var distance    = r[8].Trim();
            var kisCourseId = r[18].Trim();
            var kisMode     = r[19].Trim();
            var numStage    = r[24].Trim();
            var sandwich    = r[25].Trim();
            var title       = r[28].Trim();
            var kisAimCode  = r[33].Trim();
            var kisLevel    = r[34].Trim();

            if (!universityIdByPubUkprn.TryGetValue(pubUkprn, out var universityId))
            {
                skippedNoUniversity++;
                continue;
            }
            if (string.IsNullOrWhiteSpace(title)) continue;

            var qualLabel = kisaimLabels.GetValueOrDefault(kisAimCode);
            var fullTitle = string.IsNullOrWhiteSpace(qualLabel) ? title : $"{qualLabel} {title}";
            if (fullTitle.Length > 300) fullTitle = fullTitle[..300];

            // KISLEVEL: 03 = First degree, 04 = other undergraduate (Foundation degree/HNC/HND).
            // Discover Uni is UG-only, so nothing here ever maps to postgraduate.
            var studyLevel = kisLevel == "04" ? StudyLevel.Foundation : StudyLevel.Undergraduate;
            var mode = distance == "1" ? ProgrammeMode.Online
                     : sandwich == "1" ? ProgrammeMode.Sandwich
                     : kisMode == "02" ? ProgrammeMode.PartTime
                     : ProgrammeMode.FullTime;

            var key = (universityId, fullTitle.ToLowerInvariant(), studyLevel, mode);
            if (!existingKeys.Add(key)) continue; // already seeded (this run or a prior one)

            var courseKey = CourseKey(pubUkprn, kisCourseId, kisMode);
            var ucasCode = courseUcasCodes.GetValueOrDefault(courseKey);
            if (ucasCode is { Length: > 20 }) ucasCode = ucasCode[..20];

            var entity = new Programme
            {
                SrNo           = nextSrNo++,
                UniversityId   = universityId,
                Title          = fullTitle,
                StudyLevel     = studyLevel,
                Mode           = mode,
                DurationMonths = int.TryParse(numStage, out var stages) && stages > 0 ? stages * 12 : null,
                UcasCode       = ucasCode,
                IsActive       = true,
                DataSource     = DataSourceLabel,
                SourceUrl      = DataSourceUrl,
                LastVerifiedAt = DateTime.UtcNow
            };
            batch.Add(entity);

            if (courseSubjects.TryGetValue(courseKey, out var subjectCodes))
            {
                foreach (var code in subjectCodes.Distinct())
                {
                    if (subjectIdByCode.TryGetValue(code, out var subjectId))
                        subjectLinks.Add(new ProgrammeSubject { ProgrammeId = entity.Id, SubjectId = subjectId });
                }
            }

            if (batch.Count >= batchSize)
            {
                await FlushProgrammeBatchAsync(db, batch, subjectLinks);
                inserted += batch.Count;
                logger.LogInformation("DiscoverUniSeeder: {Inserted} programmes seeded so far...", inserted);
                batch.Clear();
                subjectLinks.Clear();
                db.ChangeTracker.Clear(); // keeps the tracker bounded across a 31k-row run
            }
        }

        if (batch.Count > 0)
        {
            await FlushProgrammeBatchAsync(db, batch, subjectLinks);
            inserted += batch.Count;
        }

        logger.LogInformation(
            "DiscoverUniSeeder: {Inserted} programmes inserted, {Skipped} skipped (no matching university).",
            inserted, skippedNoUniversity);
    }

    private static async Task FlushProgrammeBatchAsync(AdminDbContext db, List<Programme> batch, List<ProgrammeSubject> subjectLinks)
    {
        await db.Programmes.AddRangeAsync(batch);
        if (subjectLinks.Count > 0)
            await db.Set<ProgrammeSubject>().AddRangeAsync(subjectLinks);
        await db.SaveChangesAsync();
    }
}
