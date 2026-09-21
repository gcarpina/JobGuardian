namespace JobGuardian.Abstractions;

/// <summary>
/// Defines the protocol and schema versions used by JobGuardian metadata.
/// </summary>
/// <remarks>
/// The values in this type describe the compatibility contract used by JobGuardian execution and
/// persistence metadata.
/// </remarks>
public static class ProtocolVersions
{
    /// <summary>
    /// Gets the protocol version used by JobGuardian metadata.
    /// </summary>
    public const string ProtocolVersion = "1.0";

    /// <summary>
    /// Gets the schema version used by persisted JobGuardian metadata.
    /// </summary>
    public const string SchemaVersion = "1.0";
}