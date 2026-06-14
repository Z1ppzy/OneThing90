namespace OneThing90.Core;

public sealed class FocusPlan
{
    public string ThingName { get; set; } = "Главное дело";
    public string Why { get; set; } = "Каждый день двигать его вперед 90 минут.";
    public DateTime StartedOnLocal { get; set; } = DateTime.Today;
    public int DurationDays { get; set; } = 90;
    public int TargetMinutes { get; set; } = 90;

    public static FocusPlan CreateDefault()
    {
        return new FocusPlan
        {
            ThingName = "Главное дело",
            Why = "Делать то, что важно, даже после тяжелого дня.",
            StartedOnLocal = DateTime.Today,
            DurationDays = 90,
            TargetMinutes = 90
        };
    }
}
