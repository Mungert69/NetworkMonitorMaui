using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.Web.WebView2.Core;
using Microsoft.Maui.Controls;

namespace NetworkMonitor.Maui.Services
{
    internal static class WebView2Installer
    {
        public static async Task EnsureWebView2Async()
        {
#if WINDOWS
            try
            {
                // If runtime is present this returns a non-empty version string
                var ver = CoreWebView2Environment.GetAvailableBrowserVersionString(null);
                if (!string.IsNullOrEmpty(ver))
                    return;
            }
            catch
            {
                // Missing or not accessible => treat as not installed
            }

            // Ask user (UI must be available)
            bool installNow = false;
            var dispatcher = Application.Current?.Dispatcher;
            if (dispatcher != null)
            {
                await dispatcher.DispatchAsync(async () =>
                {
                    var main = Application.Current?.MainPage;
                    if (main != null)
                    {
                        installNow = await main.DisplayAlert(
                            "WebView2 runtime required",
                            "This app requires the Microsoft WebView2 runtime. Install it now?",
                            "Install", "Later");
                    }
                });
            }

            if (!installNow)
                return;

            // If the app is packaged as MSIX you can't silently run an EXE at install time.
            // Best effort: open the official download page so user can install.
            if (IsPackaged())
            {
                OpenUrl("https://developer.microsoft.com/microsoft-edge/webview2/#download-section");
                return;
            }

            // Otherwise attempt to download the small Evergreen bootstrapper and run it.
            string fwlink = RuntimeInformation.ProcessArchitecture switch
            {
                Architecture.X64 => "https://go.microsoft.com/fwlink/p/?LinkId=2124703",
                Architecture.X86 => "https://go.microsoft.com/fwlink/p/?LinkId=2124702",
                Architecture.Arm64 => "https://go.microsoft.com/fwlink/p/?LinkId=2124704",
                _ => "https://go.microsoft.com/fwlink/p/?LinkId=2124703"
            };

            var tempFile = Path.Combine(Path.GetTempPath(), "MicrosoftEdgeWebView2Bootstrapper.exe");
            try
            {
                using var http = new HttpClient();
                using var resp = await http.GetAsync(fwlink);
                resp.EnsureSuccessStatusCode();
                var bytes = await resp.Content.ReadAsByteArrayAsync();
                await File.WriteAllBytesAsync(tempFile, bytes);

                var psi = new ProcessStartInfo(tempFile, "/silent /install")
                {
                    UseShellExecute = true
                };
                Process.Start(psi);
            }
            catch
            {
                // Fallback: open the official page so user can manually select installer
                OpenUrl("https://developer.microsoft.com/microsoft-edge/webview2/#download-section");
            }
#endif
        }

        static void OpenUrl(string url)
        {
            try
            {
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            }
            catch { /* swallow */ }
        }

        // Rudimentary check for package; refine if you have Windows App SDK APIs available
        static bool IsPackaged()
        {
            try
            {
                // If you have Windows App SDK, use Windows.ApplicationModel.Package.Current.Id.Name
                // This placeholder returns false by default so the code attempts bootstrapper download.
                return false;
            }
            catch { return false; }
        }
    }
}