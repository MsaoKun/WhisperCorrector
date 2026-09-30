using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using System.Text.Encodings.Web;
using PortableWhisperCorrector.Models;

namespace PortableWhisperCorrector.Managers
{
    public class ConfigManager
    {
        private readonly string _configPath;

        public AppConfig Config { get; private set; } = new AppConfig();

        public ConfigManager()
        {
            string baseDir = AppContext.BaseDirectory;
            // 優先順位: system/config.json -> config.json -> APP/config.json
            string pSystem = Path.Combine(baseDir, "system", "config.json");
            string pRoot = Path.Combine(baseDir, "config.json");
            string pApp = Path.Combine(baseDir, "APP", "config.json");

            if (File.Exists(pSystem))
            {
                _configPath = pSystem;
            }
            else if (File.Exists(pRoot))
            {
                _configPath = pRoot;
            }
            else if (File.Exists(pApp))
            {
                _configPath = pApp;
            }
            else
            {
                _configPath = pSystem;
            }
        }

        public async Task LoadConfigAsync()
        {
            await Task.Run(() => {
                if (File.Exists(_configPath))
                {
                    try
                    {
                        string json = File.ReadAllText(_configPath);
                        Config = JsonSerializer.Deserialize<AppConfig>(json) ?? new AppConfig();
                    }
                    catch { }
                }
            });
        }

        public async Task SaveConfigAsync()
        {
            await Task.Run(() => {
                try
                {
                    string dir = Path.GetDirectoryName(_configPath);
                    if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    {
                        Directory.CreateDirectory(dir);
                    }
                    var options = new JsonSerializerOptions
                    {
                        WriteIndented = true,
                        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                    };
                    string json = JsonSerializer.Serialize(Config, options);
                    File.WriteAllText(_configPath, json);
                }
                catch { }
            });
        }
    }
}
