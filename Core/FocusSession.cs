namespace OneThing90.Core;

public sealed class FocusSession
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime StartedAtLocal { get; set; }
    public DateTime EndedAtLocal { get; set; }
    public int Minutes { get; set; }
    public bool CompletedTarget { get; set; }
    public string Note { get; set; } = string.Empty;
}
