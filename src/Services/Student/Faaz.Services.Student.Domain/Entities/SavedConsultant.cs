using Faaz.SharedKernel.Entities;

namespace Faaz.Services.Student.Domain.Entities;

// Hard-deleted on unsave (no soft-delete audit trail needed for a favorite toggle).
public class SavedConsultant : BaseEntity
{
    public SavedConsultant()
    {
        Id = RT.Comb.Provider.Sql.Create();
    }

    public Guid StudentUserId    { get; set; }
    public Guid ConsultantUserId { get; set; }
}
