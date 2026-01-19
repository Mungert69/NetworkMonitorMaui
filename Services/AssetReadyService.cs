using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NetworkMonitor.Maui;
using NetworkMonitor.Maui.Services;

namespace NetworkMonitor.Maui.Services
{
    public interface IAssetReadyService
    {
        Task EnsureAssetsReadyAsync();
        bool IsReady { get; }
        string Status { get; }
        event Action<string>? ProgressUpdated;
    }

    public sealed class AssetReadyService : IAssetReadyService
    {
        private readonly ILogger _logger;
        private readonly IConfiguration _configuration;
        private Task? _copyTask;
        private string _status = "Waiting for assets...";
        private readonly object _lockObj = new();

        public AssetReadyService(ILogger<AssetReadyService> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }

        public bool IsReady => _copyTask?.IsCompletedSuccessfully == true;
        public string Status => _status;
        public event Action<string>? ProgressUpdated;

        public Task EnsureAssetsReadyAsync()
        {
            lock (_lockObj)
            {
                _copyTask ??= CopyAssetsAsync();
            }

            return _copyTask;
        }

        private async Task CopyAssetsAsync()
        {
            try
            {
                ReportProgress("Preparing assets...");
                string opensslVersion = _configuration["OpensslVersion"] ?? "openssl";
                string os = "";
                if (OperatingSystem.IsAndroid()) os = "android";
                else if (OperatingSystem.IsWindows()) os = "windows";

                string versionStr = string.IsNullOrEmpty(os) ? opensslVersion : $"{opensslVersion}-{os}";
                var progress = new Progress<string>(ReportProgress);
                string output = await CopyAssetsHelper.CopyAssetsToLocalStorage(versionStr, "cs-assets", "dlls", progress);
                ServiceInitializer.RootProvider.AssetsReady = true;
                ReportProgress("Assets ready.");
                _logger.LogInformation("Asset copy completed. {Output}", output);
            }
            catch (Exception ex)
            {
                ReportProgress("Asset copy failed.");
                _logger.LogError(ex, "Asset copy failed.");
                throw;
            }
        }

        private void ReportProgress(string message)
        {
            _status = message;
            ProgressUpdated?.Invoke(message);
        }
    }
}
