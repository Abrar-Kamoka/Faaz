namespace Faaz.SharedKernel.Abstractions;

public interface IDateTimeProvider
{
    DateTime UtcNow { get; }
}
