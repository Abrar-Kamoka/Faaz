namespace Faaz.SharedKernel.Entities;

public abstract class BaseEntity : Trackable
{
    public Guid Id { get; protected set; }
    // Set by application code via NewSerialNumberAsync() (MAX+1). ValueGeneratedNever() in all DbContexts.
    public int SrNo { get; set; }
    public bool IsDeleted { get; set; } = false;
}
