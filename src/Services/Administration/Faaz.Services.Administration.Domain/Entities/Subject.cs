using Faaz.SharedKernel.Entities;

namespace Faaz.Services.Administration.Domain.Entities;

public class Subject : BaseSoftDeleteModel
{
    public Subject()
    {
        Id = RT.Comb.Provider.Sql.Create();
    }

    public string  Name        { get; set; } = string.Empty;
    // For HECoS-sourced rows, holds the Common Aggregation Hierarchy (CAH) group name this subject
    // rolls up to (e.g. "Computing") — HECoS itself is a flat, non-hierarchical list of ~1,092 terms.
    public string? Category    { get; set; }
    public bool    IsActive    { get; set; } = true;

    // HESA's official Higher Education Classification of Subjects code. Null for manually-added
    // subjects that don't map cleanly onto a HECoS term.
    public string?   HecosCode      { get; set; }
    public string?   DataSource     { get; set; }
    public string?   SourceUrl      { get; set; }
    public DateTime? LastVerifiedAt { get; set; }
}
