using Microsoft.Extensions.Configuration;
using NetworkMonitor.Utils.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace NetworkMonitor.Maui.Helpers
{
    public class MauiProgramHelper
    {
        private static void SetAppId(JsonObject root)
        {
            if (root.TryGetPropertyValue("AppID", out var appIdNode))
            {
                var appId = appIdNode?.ToString()?.Trim();

                if (string.Equals(appId, "usersetup", StringComparison.OrdinalIgnoreCase))
                {
                    root["AppID"] = $"{Guid.NewGuid()}-usersetup";
                }
            }
        }

        private static void SetAppName(JsonObject root, string fullAppName)
        {
            // Cross-platform: AppInfo.Current.Name works on Android, Windows, iOS, MacCatalyst


            if (!string.IsNullOrWhiteSpace(fullAppName))
            {
                root["AppName"] = GenerateShortKey(fullAppName);
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
                var fieldsToOverwrite = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                {
                    "ClientId",
                    "BaseFusionAuthURL",
                    "LoadServer",
                    "ChatServer",
                    "ServiceDomain",
                    "ServiceServer",
                    "TranscribeAudioUrl",
                    "IsChatMode",
                    "FilterStrategies",
                    "OpensslVersion",
                    "LocalSystemUrl:RabbitHostName",
                    "LocalSystemUrl:RabbitPort"
                };

                // Load the packaged configuration first
                using var stream = FileSystem.OpenAppPackageFileAsync("appsettings.json").Result;
                using var reader = new StreamReader(stream);
                var packagedJson = reader.ReadToEnd();
                var packagedRoot = JsonNode.Parse(packagedJson) as JsonObject
                    ?? throw new InvalidOperationException("Packaged configuration is not a JSON object.");

                JsonObject mergedRoot;

                if (File.Exists(localAppSettingsPath))
                {
                    // Load existing user configuration
                    var userJson = File.ReadAllText(localAppSettingsPath);
                    var userRoot = JsonNode.Parse(userJson) as JsonObject ?? new JsonObject();

                    mergedRoot = MergeAppSettings(userRoot, packagedRoot, fieldsToOverwrite, fullAppName);
                }
                else
                {
                    mergedRoot = MergeAppSettings(null, packagedRoot, fieldsToOverwrite, fullAppName);
                }

                var serialized = mergedRoot.ToJsonString(new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(localAppSettingsPath, serialized);

                config = BuildConfigurationFromJson(serialized);
            }
            catch (Exception ex)
            {
                ExceptionHelper.HandleGlobalException(ex, "Error loading appsettings.json");
            }
            builder.Configuration.AddConfiguration(config!);
        }

        internal static JsonObject MergeAppSettings(JsonObject? userRoot, JsonObject packagedRoot, IEnumerable<string> overwritePaths, string fullAppName)
        {
            if (packagedRoot is null)
            {
                throw new ArgumentNullException(nameof(packagedRoot));
            }

            if (overwritePaths is null)
            {
                overwritePaths = Array.Empty<string>();
            }

            var overwriteSet = overwritePaths as HashSet<string> ?? new HashSet<string>(overwritePaths, StringComparer.OrdinalIgnoreCase);

            JsonObject result;
            if (userRoot is not null)
            {
                result = userRoot.DeepClone() as JsonObject ?? new JsonObject();
                MergeJsonObject(result, packagedRoot, overwriteSet);
            }
            else
            {
                result = packagedRoot.DeepClone() as JsonObject ?? new JsonObject();
            }

            SetAppName(result, fullAppName);
            SetAppId(result);

            return result;
        }

        private static void MergeJsonObject(JsonObject target, JsonObject source, HashSet<string> overwritePaths, string currentPath = "")
        {
            foreach (var property in source)
            {
                var key = property.Key;
                var sourceValue = property.Value;
                var path = string.IsNullOrEmpty(currentPath) ? key : $"{currentPath}:{key}";

                if (overwritePaths.Contains(path))
                {
                    target[key] = sourceValue?.DeepClone();
                    continue;
                }

                if (!target.TryGetPropertyValue(key, out var targetValue) || targetValue is null)
                {
                    target[key] = sourceValue?.DeepClone();
                    continue;
                }

                if (targetValue is JsonObject targetObject && sourceValue is JsonObject sourceObject)
                {
                    MergeJsonObject(targetObject, sourceObject, overwritePaths, path);
                }
                // For arrays or value types we leave the user's existing value unless explicitly overwritten.
            }
        }

        private static IConfigurationRoot BuildConfigurationFromJson(string json)
        {
            return new ConfigurationBuilder()
                .AddJsonStream(new MemoryStream(Encoding.UTF8.GetBytes(json)))
                .Build();
        }
    }
}
