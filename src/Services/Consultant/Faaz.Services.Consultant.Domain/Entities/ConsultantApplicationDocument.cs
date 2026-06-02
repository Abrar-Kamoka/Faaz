using static Faaz.Services.Consultant.Domain.ConsultantEnums;
using static Faaz.SharedKernel.SharedEnums;

namespace Faaz.Services.Consultant.Domain.Entities;

public class ConsultantApplicationDocument
{
    public ConsultantApplicationDocument()
    {
        Id = RT.Comb.Provider.Sql.Create();
    }

    public Guid Id { get; set; }
    public Guid ApplicationId { get; set; }
    public ApplicationDocumentType DocumentType { get; set; }
    public string FileName { get; set; } = null!;
    public string FilePath { get; set; } = null!;
    public string ContentType { get; set; } = null!;
    public long FileSizeBytes { get; set; }
    public DateTime UploadedAt { get; set; }

    public ConsultantApplication Application { get; set; } = null!;
}
