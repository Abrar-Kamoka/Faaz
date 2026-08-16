using Faaz.SharedKernel.Entities;

namespace Faaz.Services.Consultant.Domain.Entities;

public class ConsultantCredential : BaseSoftDeleteModel
{
    public ConsultantCredential()
    {
        Id = RT.Comb.Provider.Sql.Create();
    }

    public Guid   ConsultantProfileId { get; set; }
    public string FileName            { get; set; } = string.Empty;
    public string StoredPath          { get; set; } = string.Empty;
    public string ContentType         { get; set; } = string.Empty;
    public long   FileSizeBytes       { get; set; }
    public DateTime UploadedAt        { get; set; } = DateTime.UtcNow;

    public ConsultantProfile Profile { get; set; } = null!;
}
