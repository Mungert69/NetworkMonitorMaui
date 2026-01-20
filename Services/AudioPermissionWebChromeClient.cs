#if ANDROID
using System;
using Android.Webkit;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.ApplicationModel;

namespace NetworkMonitor.Maui.Services
{
    public sealed class AudioPermissionWebChromeClient : WebChromeClient
    {
        private readonly ILogger? _logger;

        public AudioPermissionWebChromeClient(ILogger? logger = null)
        {
            _logger = logger;
        }

        public override void OnPermissionRequest(PermissionRequest? request)
        {
            if (request == null)
            {
                return;
            }

            void HandleRequest()
            {
                try
                {
                    var resources = request.GetResources() ?? Array.Empty<string>();
                    if (Array.Exists(resources, resource => resource == PermissionRequest.ResourceAudioCapture))
                    {
                        request.Grant(new[] { PermissionRequest.ResourceAudioCapture });
                        _logger?.LogInformation("WebView audio capture permission granted.");
                        return;
                    }

                    request.Deny();
                    _logger?.LogInformation("WebView permission request denied (no audio capture).");
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "WebView permission request handling failed.");
                    request.Deny();
                }
            }

            if (MainThread.IsMainThread)
            {
                HandleRequest();
            }
            else
            {
                MainThread.BeginInvokeOnMainThread(HandleRequest);
            }
        }
    }
}
#endif
