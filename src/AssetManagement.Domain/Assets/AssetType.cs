namespace AssetManagement.Domain.Assets;

/// <summary>Concrete kinds of equipment tracked by the system (used as the EF discriminator).</summary>
public enum AssetType
{
    LaserScanner = 0,
    Drone = 1,
    LaserTracker = 2,
}
