namespace OneThing90.Core;

public sealed class AppState
{
    public FocusPlan Plan { get; set; } = FocusPlan.CreateDefault();
    public AppSettings Settings { get; set; } = new();
    public List<FocusSession> Sessions { get; set; } = [];
    public DateTime? SnoozedUntilLocal { get; set; }
    public DateTime? LastReminderLocal { get; set; }
}
