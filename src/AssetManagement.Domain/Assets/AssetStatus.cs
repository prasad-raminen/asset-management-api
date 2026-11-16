namespace AssetManagement.Domain.Assets;

public enum AssetStatus
{
    Available = 0,
    CheckedOut = 1,
    UnderMaintenance = 2,
    Retired = 3,
}
