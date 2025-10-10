using System.Text.Json.Nodes;
using NetworkMonitor.Maui.Helpers;

namespace NetworkMonitorMaui.Tests;

public class MauiProgramHelperTests
{
    private static readonly string[] DefaultOverwriteSet =
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

    [Fact]
    public void MergeAppSettings_WithExistingUserConfig_AppliesOverwritePaths()
    {
        var userJson = """
        {
          "LocalSystemUrl": {
            "ExternalUrl": "http://legacy-host",
            "RabbitHostName": "user-rabbit",
            "RabbitPort": 4321
          },
          "AppID": "usersetup",
          "Owner": "UserOwner",
          "FilterStrategies": [
            { "StrategyName": "custom", "EndpointTypeContains": ["legacy"] }
          ]
        }
        """;

        var packagedJson = """
        {
          "LocalSystemUrl": {
            "ExternalUrl": "https://packaged-host",
            "RabbitHostName": "packaged-rabbit",
            "RabbitPort": 9988,
            "RabbitVHost": "/vhostuser"
          },
          "AppID": "usersetup",
          "Owner": "PackagedOwner",
          "FilterStrategies": [
            {
              "StrategyName": "skip5",
              "EndpointTypeContains": ["quantum"],
              "FireInterval": { "Mode": "counter", "Every": 5, "Offset": 0 }
            },
            {
              "StrategyName": "daily",
              "EndpointTypeContains": ["daily"],
              "FireInterval": { "Mode": "daily-slot", "SlotsPerDay": 24 }
            }
          ],
          "DisabledCommands": [ "legacy" ]
        }
        """;

        var userRoot = JsonNode.Parse(userJson) as JsonObject;
        var packagedRoot = (JsonNode.Parse(packagedJson) as JsonObject)!;

        var merged = MauiProgramHelper.MergeAppSettings(userRoot, packagedRoot, DefaultOverwriteSet, "Quantum Secure Monitor");

        var localSystem = (JsonObject)merged["LocalSystemUrl"]!;
        Assert.Equal("http://legacy-host", localSystem["ExternalUrl"]!.GetValue<string>());
        Assert.Equal("packaged-rabbit", localSystem["RabbitHostName"]!.GetValue<string>());
        Assert.Equal(9988, localSystem["RabbitPort"]!.GetValue<int>());
        Assert.Equal("/vhostuser", localSystem["RabbitVHost"]!.GetValue<string>());

        var strategies = (JsonArray?)merged["FilterStrategies"];
        Assert.NotNull(strategies);
        Assert.Equal(packagedRoot["FilterStrategies"]!.ToJsonString(), strategies!.ToJsonString());

        Assert.Equal("UserOwner", merged["Owner"]!.GetValue<string>());

        var appId = merged["AppID"]!.GetValue<string>();
        Assert.NotEqual("usersetup", appId);
        Assert.EndsWith("-usersetup", appId);
        Assert.True(Guid.TryParse(appId.Replace("-usersetup", string.Empty), out _));

        Assert.Equal("QSMX", merged["AppName"]!.GetValue<string>());
    }

    [Fact]
    public void MergeAppSettings_WithExistingUserConfig_PreservesNonOverwrittenArrays()
    {
        var userJson = """
        {
          "DisabledCommands": ["smtp", "nmap"]
        }
        """;

        var packagedJson = """
        {
          "DisabledCommands": [],
          "FilterStrategies": [
            {
              "StrategyName": "skip10",
              "EndpointTypeContains": ["smtp"],
              "FireInterval": { "Mode": "counter", "Every": 10, "Offset": 1 }
            }
          ]
        }
        """;

        var userRoot = JsonNode.Parse(userJson) as JsonObject;
        var packagedRoot = (JsonNode.Parse(packagedJson) as JsonObject)!;

        var merged = MauiProgramHelper.MergeAppSettings(userRoot, packagedRoot, DefaultOverwriteSet, "Quantum Secure Monitor");

        var disabledCommands = (JsonArray?)merged["DisabledCommands"];
        Assert.NotNull(disabledCommands);
        Assert.Equal(userRoot!["DisabledCommands"]!.ToJsonString(), disabledCommands!.ToJsonString());

        var strategies = (JsonArray?)merged["FilterStrategies"];
        Assert.NotNull(strategies);
        Assert.Equal(packagedRoot["FilterStrategies"]!.ToJsonString(), strategies!.ToJsonString());
    }

    [Fact]
    public void MergeAppSettings_FirstRun_UsesPackagedValues()
    {
        var packagedJson = """
        {
          "AppID": "usersetup",
          "FilterStrategies": [
            { "StrategyName": "daily", "EndpointTypeContains": ["daily"] }
          ],
          "LocalSystemUrl": {
            "RabbitHostName": "packaged-rabbit",
            "RabbitPort": 5672
          }
        }
        """;

        var packagedRoot = (JsonNode.Parse(packagedJson) as JsonObject)!;

        var merged = MauiProgramHelper.MergeAppSettings(null, packagedRoot, DefaultOverwriteSet, "Quantum Secure Monitor");

        // Ensure original packaged root remains unchanged
        Assert.Equal("usersetup", packagedRoot["AppID"]!.GetValue<string>());

        var appId = merged["AppID"]!.GetValue<string>();
        Assert.NotEqual("usersetup", appId);
        Assert.EndsWith("-usersetup", appId);

        Assert.Equal("QSMX", merged["AppName"]!.GetValue<string>());

        var strategies = (JsonArray?)merged["FilterStrategies"];
        Assert.NotNull(strategies);
        Assert.Single(strategies!);
        Assert.Equal("daily", ((JsonObject)strategies[0]!)["StrategyName"]!.GetValue<string>());
    }

    [Fact]
    public void MergeAppSettings_AppIdNotUserSetup_RemainsUntouched()
    {
        var userJson = """
        {
          "AppID": "custom-app-id"
        }
        """;

        var packagedJson = """
        {
          "AppID": "usersetup"
        }
        """;

        var userRoot = JsonNode.Parse(userJson) as JsonObject;
        var packagedRoot = (JsonNode.Parse(packagedJson) as JsonObject)!;

        var merged = MauiProgramHelper.MergeAppSettings(userRoot, packagedRoot, DefaultOverwriteSet, "Quantum Secure Monitor");

        Assert.Equal("custom-app-id", merged["AppID"]!.GetValue<string>());
    }
}
