using System.Globalization;

namespace ToolTracking.Tests;

internal static class TestData
{
    public const string Wrench1 = "3034F4E4E40C0E40000003E9";
    public const string Wrench2 = "3034F4E4E40C0E40000003EA";
    public const string Caliper = "3034F4E4E4424C80000007D1";

    public static readonly DateTimeOffset T0 = At("2026-03-02T08:00:00Z");

    public static DateTimeOffset At(string iso) =>
        DateTimeOffset.Parse(iso, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal);

    public static ReaderConfiguration Readers() => new(new[]
    {
        new ReaderAntenna("WH-01", 1, "Warehouse"),
        new ReaderAntenna("WH-01", 2, "Warehouse"),
        new ReaderAntenna("PROD-01", 1, "Production"),
        new ReaderAntenna("LAB-01", 1, "CalibrationLab"),
    });

    public static TagRead Read(string epc, string zone, double secondsAfterT0, double rssi = -60) =>
        new(T0.AddSeconds(secondsAfterT0), "TEST", 1, epc, rssi, zone);

    public static Sighting SightingOf(string epc, string zone, double firstSecond, double lastSecond) =>
        new(epc, zone, T0.AddSeconds(firstSecond), T0.AddSeconds(lastSecond), 1, -60);
}
