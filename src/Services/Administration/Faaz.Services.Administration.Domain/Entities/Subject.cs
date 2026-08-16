using Faaz.SharedKernel.Entities;

namespace Faaz.Services.Administration.Domain.Entities;

public class Subject : BaseSoftDeleteModel
{
    public Subject()
    {
        Id = RT.Comb.Provider.Sql.Create();
    }

    public string  Name        { get; set; } = string.Empty;
    public string? Category    { get; set; }
    public bool    IsActive    { get; set; } = true;
}
