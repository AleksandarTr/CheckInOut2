using System.Collections.Generic;
using System.IO;

namespace CheckInOut2.Models;

public static class Settings {
    private static Dictionary<string, string> settings = new Dictionary<string,string>();
    private const string SettingsPath = "settings.config";
    public static void save() {
        StreamWriter writer = new StreamWriter(SettingsPath, false);
        foreach(var setting in settings) writer.WriteLine(setting.Key + "=" + setting.Value);
        writer.Close();
    }

    private static bool loadDefaultSettings() {
        Dictionary<string, string> defaultSettings = new Dictionary<string, string>() {
            {"readerID", "0"},
            {"fontSize", "16"},
            {"toleranceEarly", "10"},
            {"toleranceLate", "5"}
        };
        bool changed = false;

        foreach(var setting in defaultSettings)
            if(!settings.ContainsKey(setting.Key)) {
                settings.Add(setting.Key, setting.Value);
                changed = true;
            }

        return changed;
    }

    static Settings() {
        try {
            StreamReader reader = new StreamReader(SettingsPath);
            string? line = reader.ReadLine();
            while(line != null) {
                string[] setting = line.Split('=', 2);
                settings.Add(setting[0], setting[1]);
                line = reader.ReadLine();
            }
            reader.Close();
            if(loadDefaultSettings()) save();
        }
        catch(FileNotFoundException) {
            loadDefaultSettings();
            save();
        }
    }

    public static bool set(string key, string value) {
        if(!settings.ContainsKey(key)) return false;
        settings[key] = value;
        return true;
    }

    public static string? get(string key) {
        if(settings.ContainsKey(key)) return settings[key];
        return null;
    }

    public static int? getInt(string key) {
        if(settings.ContainsKey(key) && int.TryParse(settings[key], out int result)) return result;
        return null;
    }
}