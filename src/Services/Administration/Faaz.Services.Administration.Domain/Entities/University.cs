using Faaz.SharedKernel.Entities;

namespace Faaz.Services.Administration.Domain.Entities;

public class University : BaseSoftDeleteModel
{
    public University()
    {
        Id = RT.Comb.Provider.Sql.Create();
    }

    public string  Name        { get; set; } = string.Empty;
    public string? Country     { get; set; }
    public string? LogoUrl     { get; set; }
    public bool    IsActive    { get; set; } = true;

    // UK Register of Learning Providers / HESA provider reference — the official 8-digit identifier
    // for a UK HE provider. Null for manually-added or non-UK rows that don't have one.
    public string?   Ukprn           { get; set; }
    public string?   Nation          { get; set; } // England / Scotland / Wales / NorthernIreland / Overseas
    public string?   City            { get; set; }
    public string?   InstitutionType { get; set; } // University / FE College / Conservatoire / Specialist
    public bool      IsRussellGroup  { get; set; } = false;
    public string?   WebsiteUrl      { get; set; }

    // Provenance — where this row came from and how fresh it is, so imported data never goes
    // silently stale and admins can tell curated rows apart from bulk-imported ones.
    public string?   DataSource      { get; set; }
    public string?   SourceUrl       { get; set; }
    public DateTime? LastVerifiedAt  { get; set; }
}
