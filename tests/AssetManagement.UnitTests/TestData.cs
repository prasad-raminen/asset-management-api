using AssetManagement.Domain.Assets;

namespace AssetManagement.UnitTests;

/// <summary>Builders for domain objects used across tests.</summary>
internal static class TestData
{
    public static readonly DateTime Now = new(2025, 3, 1, 9, 0, 0, DateTimeKind.Utc);

    public static LaserScanner Scanner(DateTime? registeredAt = null) =>
        new("RTC360 #1", "RTC-0001", "Leica Geosystems", "RTC360", "Depot A", 130, registeredAt ?? Now);

    public static Drone Drone(DateTime? registeredAt = null) =>
        new("Drone Alpha", "UAV-0001", "DJI", "Matrice 350 RTK", "Depot A", 55, registeredAt ?? Now);

    public static LaserTracker Tracker(DateTime? registeredAt = null) =>
        new("Tracker #1", "AT-0001", "Leica Geosystems", "AT960", "Lab", 15, registeredAt ?? Now);
}
