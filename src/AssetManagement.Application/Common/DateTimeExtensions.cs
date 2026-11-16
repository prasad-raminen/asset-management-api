namespace AssetManagement.Application.Common;

public static class DateTimeExtensions
{
    /// <summary>
    /// Normalises a client-supplied timestamp to UTC. Values without an offset are treated as UTC
    /// (the API contract is "*Utc" fields), never as the server's local time.
    /// </summary>
    public static DateTime AsUtc(this DateTime value) => value.Kind switch
    {
        DateTimeKind.Utc => value,
        DateTimeKind.Local => value.ToUniversalTime(),
        _ => DateTime.SpecifyKind(value, DateTimeKind.Utc),
    };
}
