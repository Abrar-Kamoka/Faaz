namespace Faaz.Services.Identity.WebHost.HttpClients;

public interface IStudentServiceClient
{
    Task CreateProfileStubAsync(Guid userId, string email, string firstName, string lastName, CancellationToken ct = default);
}
