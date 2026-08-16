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
}
