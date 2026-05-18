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
        private volatile bool _isReady;
        private string _status = "Waiting for assets...";
        private readonly object _lockObj = new();

        public AssetReadyService(ILogger<AssetReadyService> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }

        public bool IsReady => _isReady;
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
                var progress = new InlineProgress(ReportProgress);
                var dllAssetDir = OperatingSystem.IsWindows() ? "windowsdlls" : "dlls";
                string output = await CopyAssetsHelper.CopyAssetsToLocalStorage(versionStr, "cs-assets", dllAssetDir, progress);
                ServiceInitializer.RootProvider.AssetsReady = true;
                _isReady = true;
                ReportProgress("Assets ready.");
                _logger.LogInformation("Asset copy completed. Output length: {OutputLength}", output.Length);
            }
            catch (Exception ex)
            {
                _isReady = false;
                ReportProgress("Asset copy failed.");
                _logger.LogError(ex, "Asset copy failed.");
                throw;
            }
        }

        private void ReportProgress(string message)
        {
            if (string.Equals(_status, message, StringComparison.Ordinal))
            {
                return;
            }

            _status = message;
            ProgressUpdated?.Invoke(message);
        }

        private sealed class InlineProgress : IProgress<string>
        {
            private readonly Action<string> _report;

            public InlineProgress(Action<string> report)
            {
                _report = report;
            }

            public void Report(string value)
            {
                _report(value);
            }
        }
    }
}
