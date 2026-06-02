namespace Faaz.SharedKernel.Exceptions;

public sealed class ForbiddenException : Exception
{
    public string ErrorType { get; }

    public ForbiddenException(string errorType, string message)
        : base(message)
    {
        ErrorType = errorType;
    }

    public ForbiddenException(string message)
        : base(message)
    {
        ErrorType = "forbidden";
    }
}
