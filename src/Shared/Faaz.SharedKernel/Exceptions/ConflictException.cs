namespace Faaz.SharedKernel.Exceptions;

public sealed class ConflictException : Exception
{
    public object? Payload { get; }

    public ConflictException(string message, object? payload = null)
        : base(message) => Payload = payload;
}
