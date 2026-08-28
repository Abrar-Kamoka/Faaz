using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Faaz.BuildingBlocks.Persistence;

// SQL Server's datetime2 carries no timezone info, so EF Core reads every DateTime back as
// Kind=Unspecified even though every DateTime column in this app is UTC by convention (the ScheduledStartUtc/
// CreatedAt/etc. naming makes that explicit). System.Text.Json only appends the 'Z' suffix for
// Kind=Utc — anything else serializes as an offset-less string like "2026-08-26T09:30:00", and
// browsers parse an offset-less ISO string as LOCAL time. That silently shifts every date the
// frontend touches (countdowns, join-window checks, displayed times) by the viewer's UTC offset,
// and only looks correct by coincidence for a viewer who happens to be in UTC+0.
internal sealed class UtcDateTimeConverter : ValueConverter<DateTime, DateTime>
{
    public UtcDateTimeConverter() : base(
        v => v,
        v => DateTime.SpecifyKind(v, DateTimeKind.Utc))
    {
    }
}

internal sealed class UtcNullableDateTimeConverter : ValueConverter<DateTime?, DateTime?>
{
    public UtcNullableDateTimeConverter() : base(
        v => v,
        v => v.HasValue ? DateTime.SpecifyKind(v.Value, DateTimeKind.Utc) : v)
    {
    }
}

public static class UtcDateTimeConventionExtensions
{
    /// <summary>
    /// Stamps Kind=Utc on every DateTime/DateTime? read from the database, model-wide. Call from
    /// each DbContext's ConfigureConventions override — fixes this at the source for every entity,
    /// instead of patching each DTO/serializer (or every frontend call site) individually.
    /// </summary>
    public static void ConfigureUtcDateTimeConvention(this ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<DateTime>().HaveConversion<UtcDateTimeConverter>();
        configurationBuilder.Properties<DateTime?>().HaveConversion<UtcNullableDateTimeConverter>();
    }
}
