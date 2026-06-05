namespace Faaz.Services.Student.WebHost.HttpClients;

public interface IIdentityServiceClient
{
    Task UpdateUserNameAsync(Guid userId, string firstName, string lastName, CancellationToken ct = default);
}
