namespace Faaz.SharedKernel.Exceptions;

public sealed class BusinessRuleException : Exception
{
    public string UserMessageCode { get; }
    public ErrorLevel Level { get; }
    public Dictionary<string, string>? FieldInfos { get; }

    private BusinessRuleException(string message, string userMessageCode, ErrorLevel level, Dictionary<string, string>? fieldInfos)
        : base(message)
    {
        UserMessageCode = userMessageCode;
        Level = level;
        FieldInfos = fieldInfos;
    }

    public static BusinessRuleException Error(string message, string userMessageCode, Dictionary<string, string>? fieldInfos = null)
    {
        return new BusinessRuleException(message, userMessageCode, ErrorLevel.Error, fieldInfos);
    }

    public static BusinessRuleException Warning(string message, string userMessageCode)
    {
        return new BusinessRuleException(message, userMessageCode, ErrorLevel.Warning, null);
    }

    public static BusinessRuleException Info(string message, string userMessageCode)
    {
        return new BusinessRuleException(message, userMessageCode, ErrorLevel.Info, null);
    }
}
