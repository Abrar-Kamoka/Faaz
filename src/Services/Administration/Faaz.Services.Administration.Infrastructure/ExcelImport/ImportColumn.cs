namespace Faaz.Services.Administration.Infrastructure.ExcelImport;

// Column metadata the generic engine needs to draw a template sheet (header, width, dropdown data
// validation) without any per-entity ClosedXML code — the entity-specific part is only "what are
// my columns and how do I map a row of these back to my entity", which lives in each descriptor.
public sealed class ImportColumn
{
    public required string   Header          { get; init; }
    public bool               Required        { get; init; }
    public string[]?          DropdownOptions { get; init; }
    public double              Width           { get; init; } = 22;
}
