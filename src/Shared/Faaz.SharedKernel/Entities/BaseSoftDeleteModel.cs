namespace Faaz.SharedKernel.Entities;

public abstract class BaseSoftDeleteModel : BaseEntity
{
    public Guid? DeletedBy { get; set; }
    public DateTime? DeletedAt { get; set; }
}
