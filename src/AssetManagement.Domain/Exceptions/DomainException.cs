namespace AssetManagement.Domain.Exceptions;

/// <summary>
/// Raised when an operation would violate a business invariant of the domain model.
/// </summary>
public class DomainException : Exception
{
    public DomainException(string code, string message)
        : base(message)
    {
        Code = code;
    }

    /// <summary>A stable, machine-readable error code (e.g. <c>asset.already_checked_out</c>).</summary>
    public string Code { get; }
}
