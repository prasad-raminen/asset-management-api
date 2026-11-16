namespace AssetManagement.Application.Common.Exceptions;

/// <summary>The request conflicts with the current state of a resource. Mapped to HTTP 409.</summary>
public sealed class ConflictException : Exception
{
    public ConflictException(string code, string message)
        : base(message)
    {
        Code = code;
    }

    public ConflictException(string code, string message, Exception innerException)
        : base(message, innerException)
    {
        Code = code;
    }

    public string Code { get; }
}
