namespace AssetManagement.Domain.Users;

public enum UserRole
{
    /// <summary>Read-only access to the asset register.</summary>
    Viewer = 0,

    /// <summary>Can check equipment in/out and log maintenance.</summary>
    Technician = 1,

    /// <summary>Full access including asset and user administration.</summary>
    Admin = 2,
}
