using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using NetworkMonitor.Connection;
using NetworkMonitor.Maui.Services;

namespace NetworkMonitorMaui.Tests;

public abstract class ViewModelTestBase
{
    static ViewModelTestBase()
    {
        ServiceInitializer.ResetForTests();
        ServiceInitializer.Initialize(new TestRootNamespaceProvider(), new TestDispatcher());
    }

    protected static NetConnectConfig CreateNetConnectConfig(Dictionary<string, string?>? overrides = null)
    {
        var defaults = new Dictionary<string, string?>
        {
            ["BaseFusionAuthURL"] = "https://auth.example.com",
            ["ClientId"] = "client-123",
            ["LocalSystemUrl:RabbitHostName"] = "localhost",
            ["LocalSystemUrl:RabbitPort"] = "5672",
            ["LocalSystemUrl:RabbitInstanceName"] = "instance",
            ["LocalSystemUrl:ExternalUrl"] = "https://example.com",
            ["LocalSystemUrl:RabbitUserName"] = "user",
            ["LocalSystemUrl:RabbitPassword"] = "password",
            ["LocalSystemUrl:IPAddress"] = "127.0.0.1",
            ["AppID"] = "app-123",
            ["ClientAuthUrl"] = "https://auth.example.com/device",
            ["Owner"] = "owner",
            ["MonitorLocation"] = "TestLocation"
        };

        if (overrides != null)
        {
            foreach (var kvp in overrides)
            {
                defaults[kvp.Key] = kvp.Value;
            }
        }

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(defaults!)
            .Build();

        var netConfig = new NetConnectConfig(configuration, Path.GetTempPath());
        return netConfig;
    }

    protected static NullLogger<T> GetLogger<T>() where T : class => new();
}
