using System.IO;
using System.Text.Json;
using OneThing90.Core;

namespace OneThing90.Services;

public sealed class StateStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    public string AppDirectory { get; }
    public string StatePath { get; }

    public StateStore()
    {
        AppDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "OneThing90");
        StatePath = Path.Combine(AppDirectory, "state.json");
    }

    public AppState Load()
    {
        try
        {
            if (!File.Exists(StatePath))
            {
                return new AppState();
            }

            var json = File.ReadAllText(StatePath);
            return JsonSerializer.Deserialize<AppState>(json, JsonOptions) ?? new AppState();
        }
        catch
        {
            return new AppState();
        }
    }

    public void Save(AppState state)
    {
        Directory.CreateDirectory(AppDirectory);

        var tempPath = StatePath + ".tmp";
        var json = JsonSerializer.Serialize(state, JsonOptions);
        File.WriteAllText(tempPath, json);
        File.Move(tempPath, StatePath, overwrite: true);
    }
}
