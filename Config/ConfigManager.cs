using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using NAudio.CoreAudioApi;

namespace DiscordAudioGuardTray.Config
{
    public static class ConfigManager
    {
        private static readonly string ConfigPath =
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");
        private static RootConfig _config = null!;

        static ConfigManager()
        {
            LoadConfig();
        }

        public static AppConfig Config => _config.AppConfig;

        public static void LoadConfig()
        {
            try
            {
                if (File.Exists(ConfigPath))
                {
                    string json = File.ReadAllText(ConfigPath);
                    var config = JsonSerializer.Deserialize<RootConfig>(json);

                    if (config != null)
                    {
                        _config = config;

                        // Migrate from old format to new format
                        MigrateOldConfig();
                    }
                    else
                    {
                        _config = new RootConfig();
                    }
                }
                else
                {
                    _config = new RootConfig();
                }

                // Update device list when loading config
                UpdateDeviceList();

                SaveConfig(); // Save current config
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading config: {ex.Message}");
                _config = new RootConfig();
            }
        }

        private static void MigrateOldConfig()
        {
            // Check if old configuration format exists
            try
            {
                string json = File.ReadAllText(ConfigPath);

                // If JSON contains "IgnoredDevices" array, it's the old format
                if (json.Contains("\"IgnoredDevices\""))
                {
                    var oldConfig = JsonSerializer.Deserialize<OldConfig>(json);
                    if (oldConfig?.AppConfig?.IgnoredDevices != null)
                    {
                        // Transfer old settings to new format
                        foreach (var deviceName in oldConfig.AppConfig.IgnoredDevices)
                        {
                            // Find or create device config
                            var device = _config.AppConfig.Devices
                                .FirstOrDefault(d => d.FriendlyName == deviceName);

                            if (device != null)
                            {
                                device.Ignored = true;
                            }
                        }

                        Console.WriteLine("Settings migration completed successfully");
                    }
                }
            }
            catch
            {
                // Ignore migration errors
            }
        }

        private static void UpdateDeviceList()
        {
            try
            {
                using (var enumerator = new MMDeviceEnumerator())
                {
                    var systemDevices = enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.All);

                    // For each system device
                    foreach (var systemDevice in systemDevices)
                    {
                        var existingDevice = _config.AppConfig.Devices
                            .FirstOrDefault(d => d.Id == systemDevice.ID);

                        if (existingDevice == null)
                        {
                            // Add new device
                            _config.AppConfig.Devices.Add(new DeviceConfig
                            {
                                Id = systemDevice.ID,
                                FriendlyName = systemDevice.FriendlyName,
                                Ignored = false
                            });
                        }
                        else
                        {
                            // Update device name if it changed
                            existingDevice.FriendlyName = systemDevice.FriendlyName;
                        }
                    }

                    // Remove devices that no longer exist in the system
                    var deviceIds = systemDevices.Select(d => d.ID).ToList();
                    _config.AppConfig.Devices.RemoveAll(d => !deviceIds.Contains(d.Id));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating device list: {ex.Message}");
            }
        }

        public static void SaveConfig()
        {
            try
            {
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                };

                string json = JsonSerializer.Serialize(_config, options);
                File.WriteAllText(ConfigPath, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving config: {ex.Message}");
            }
        }

        public static void UpdateConfig(Action<AppConfig> updateAction)
        {
            updateAction?.Invoke(_config.AppConfig);
            SaveConfig();
        }

        // Class for deserializing old config format
        private class OldConfig
        {
            public OldAppConfig? AppConfig { get; set; }
        }

        private class OldAppConfig
        {
            public string[]? IgnoredDevices { get; set; }
        }
    }
}
