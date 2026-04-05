using System.Text.Json;
using Godot;
using Sts2Trainer.Shared;

namespace Sts2Trainer.Mod.Runtime.Services;

internal sealed class TrainerSettingsStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    public TrainerSettings Load()
    {
        try
        {
            var path = GetSettingsPath();
            if (!File.Exists(path))
            {
                return new TrainerSettings();
            }

            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<TrainerSettings>(json, JsonOptions) ?? new TrainerSettings();
        }
        catch (Exception ex)
        {
            TrainerLog.Error($"Failed to load settings: {ex.Message}");
            return new TrainerSettings();
        }
    }

    public void Save(TrainerSettings settings)
    {
        try
        {
            var path = GetSettingsPath();
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            var json = JsonSerializer.Serialize(settings, JsonOptions);
            File.WriteAllText(path, json);
        }
        catch (Exception ex)
        {
            TrainerLog.Error($"Failed to save settings: {ex.Message}");
        }
    }

    public string GetSettingsPath()
    {
        var baseDir = ProjectSettings.GlobalizePath("user://");
        return Path.Combine(baseDir, TrainerConstants.SettingsDirectoryName, TrainerConstants.SettingsFileName);
    }

    public string GetLogDirectory()
    {
        var baseDir = ProjectSettings.GlobalizePath("user://");
        return Path.Combine(baseDir, TrainerConstants.SettingsDirectoryName, TrainerConstants.LogDirectoryName);
    }
}
