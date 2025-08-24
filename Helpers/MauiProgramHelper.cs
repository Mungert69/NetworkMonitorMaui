using Microsoft.Extensions.Configuration;
using NetworkMonitor.Utils.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace NetworkMonitor.Maui.Helpers
{
    public class MauiProgramHelper
    {
        private static void SetAppId(Dictionary<string, object> dict)
        {
            if (dict.TryGetValue("AppID", out var appIdObj))
            {
                var appId = appIdObj?.ToString()?.Trim();

                if (string.Equals(appId, "usersetup", StringComparison.OrdinalIgnoreCase))
                {
                    dict["AppID"] = $"{Guid.NewGuid()}-usersetup";
                }
            }
        }

        private static void SetAppName(Dictionary<string, object> dict, string fullAppName)
        {
            // Cross-platform: AppInfo.Current.Name works on Android, Windows, iOS, MacCatalyst


            if (!string.IsNullOrWhiteSpace(fullAppName))
            {
                dict["AppName"] = GenerateShortKey(fullAppName);
            }
        }
        public static string GenerateShortKey(string appName)
        {
            var words = appName.Split(new[] { ' ', '-', '_' },
                                      StringSplitOptions.RemoveEmptyEntries);

            var initials = words.Select(w => char.ToUpperInvariant(w[0]));
            var key = new string(initials.Take(4).ToArray());

            return key.PadRight(4, 'X'); // ensure length is 4
        }


        public static void LoadConfiguration(MauiAppBuilder builder, string fullAppName)
        {
            IConfigurationRoot? config = null;


            try
            {
                string localAppSettingsPath = Path.Combine(FileSystem.AppDataDirectory, "appsettings.json");

                // List of fields that should always be overwritten by the packaged version
                var fieldsToOverwrite = new List<string>
        {
            "ClientId",
            "BaseFusionAuthURL",
            "LoadServer",
            "ChatServer",
            "ServiceDomain",
            "ServiceServer",
            "TranscribeAudioUrl",
            "IsChatMode",
            "OpensslVersion",
            "LocalSystemUrl:RabbitHostName",
            "LocalSystemUrl:RabbitPort"

        };

                // Load the packaged configuration first
                using var stream = FileSystem.OpenAppPackageFileAsync("appsettings.json").Result;
                IConfigurationRoot packagedConfig = new ConfigurationBuilder()
                    .AddJsonStream(stream)
                    .Build();

                // Convert to dictionary for easier comparison
                var packagedDict = GetConfigDictionary(packagedConfig);

                if (File.Exists(localAppSettingsPath))
                {
                    // Load existing user configuration
                    IConfigurationRoot userConfig = new ConfigurationBuilder()
                        .AddJsonFile(localAppSettingsPath, optional: false, reloadOnChange: false)
                        .Build();

                    var userDict = GetConfigDictionary(userConfig);

                    // Process all fields
                    foreach (var kvp in packagedDict)
                    {
                        if (!userDict.ContainsKey(kvp.Key))
                        {
                            // Add new field if it doesn't exist in user config
                            userDict[kvp.Key] = kvp.Value;
                        }
                        else if (fieldsToOverwrite.Contains(kvp.Key))
                        {
                            // Overwrite the field if it's in our overwrite list
                            userDict[kvp.Key] = kvp.Value;
                        }
                        // Existing fields not in the overwrite list remain unchanged
                    }

                    SetAppName(userDict, fullAppName);
                    SetAppId(userDict);


                    // Save the augmented configuration
                    File.WriteAllText(localAppSettingsPath,
                        JsonSerializer.Serialize(userDict, new JsonSerializerOptions { WriteIndented = true }));
                    config = new ConfigurationBuilder()
                            .AddInMemoryCollection(ConvertToKeyValuePairs(userDict))
                            .Build();
                }
                else
                {
                    // First run - just use the packaged config
                    SetAppName(packagedDict, fullAppName);
                    SetAppId(packagedDict);

                    File.WriteAllText(localAppSettingsPath,
                        JsonSerializer.Serialize(packagedDict, new JsonSerializerOptions { WriteIndented = true }));

                    // Build config from the modified dictionary, not the old packagedConfig
                    config = new ConfigurationBuilder()
                        .AddInMemoryCollection(ConvertToKeyValuePairs(packagedDict))
                        .Build();

                }
            }
            catch (Exception ex)
            {
                ExceptionHelper.HandleGlobalException(ex, "Error loading appsettings.json");
            }
            builder.Configuration.AddConfiguration(config!);
        }

        // Helper to convert configuration to flat dictionary
        private static Dictionary<string, object> GetConfigDictionary(IConfiguration config)
        {
            var dict = new Dictionary<string, object>();
            void RecurseChildren(IEnumerable<IConfigurationSection> children, string prefix = "")
            {
                foreach (var child in children)
                {
                    var key = string.IsNullOrEmpty(prefix) ? child.Key : $"{prefix}:{child.Key}";

                    if (child.Value == null && child.GetChildren().Any())
                    {
                        // This is a section node with children
                        RecurseChildren(child.GetChildren(), key);
                    }
                    else
                    {
                        // This is a value node
                        dict[key] = child.Value;
                    }
                }
            }

            RecurseChildren(config.GetChildren());
            return dict;
        }

        // Helper to convert dictionary back to key-value pairs for ConfigurationBuilder
        private static IEnumerable<KeyValuePair<string, string>> ConvertToKeyValuePairs(Dictionary<string, object> dict)
        {
            foreach (var kvp in dict)
            {
                yield return new KeyValuePair<string, string>(kvp.Key, kvp.Value?.ToString());
            }
        }
    }
}
