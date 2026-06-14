namespace OneThing90.Core;

public sealed class FocusPlan
{
    public string ThingName { get; set; } = "One important thing";
    public string Why { get; set; } = "Build it for 90 focused days.";
    public DateTime StartedOnLocal { get; set; } = DateTime.Today;
    public int DurationDays { get; set; } = 90;
    public int TargetMinutes { get; set; } = 90;

    public static FocusPlan CreateDefault()
    {
        return new FocusPlan
        {
            ThingName = "One important thing",
            Why = "Show up for the work that matters.",
            StartedOnLocal = DateTime.Today,
            DurationDays = 90,
            TargetMinutes = 90
        };
    }
}
