using System.Text.Json;
using KeyboardMouseOverlay.Models;

namespace KeyboardMouseOverlay.Services;

public static class SettingsService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
    };

    private static string SettingsPath => System.IO.Path.Combine(AppContext.BaseDirectory, "settings.json");

    public static OverlaySettings Load()
    {
        try
        {
            if (!System.IO.File.Exists(SettingsPath))
            {
                return new OverlaySettings();
            }

            var json = System.IO.File.ReadAllText(SettingsPath);
            return JsonSerializer.Deserialize<OverlaySettings>(json, JsonOptions) ?? new OverlaySettings();
        }
        catch
        {
            return new OverlaySettings();
        }
    }

    public static void Save(OverlaySettings settings)
    {
        try
        {
            var json = JsonSerializer.Serialize(settings, JsonOptions);
            System.IO.File.WriteAllText(SettingsPath, json);
        }
        catch
        {
            // Ignore save errors and keep the app running.
        }
    }
}
