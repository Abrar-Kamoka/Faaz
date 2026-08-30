using Faaz.SharedKernel.Entities;

namespace Faaz.Services.Administration.Domain.Entities;

// Admin-editable, standardized taxonomy of consultancy services — replaces the two independently-
// duplicated hardcoded enums ConsultantEnums.ServiceType (what a consultant offers) and
// StudentEnums.HelpType (what a student is looking for). One shared vocabulary for both sides of
// the marketplace, extensible without a code deploy.
public class Service : BaseSoftDeleteModel
{
    public Service()
    {
        Id = RT.Comb.Provider.Sql.Create();
    }

    public string  Name        { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Category    { get; set; }
    public int     SortOrder   { get; set; } = 0;
    public bool    IsActive    { get; set; } = true;
}
