#if ANDROID
using Android.App;
using Android.Content;
using Android.OS;
using Android.Graphics;
using AndroidX.Core.App;
using Microsoft.Extensions.Logging;
using NetworkMonitor.Connection;
using NetworkMonitor.Processor.Services;
using NetworkMonitor.Objects.Repository;
using NetworkMonitor.Objects.ServiceMessage;
using NetworkMonitor.Utils.Helpers;
using NetworkMonitor.DTOs;
using NetworkMonitor.Objects;
using Microsoft.Extensions.Configuration;
using NetworkMonitor.Maui.Services;
using NetworkMonitor.Maui;


namespace NetworkMonitor.Maui.Services
{

    [Android.App.Service(ForegroundServiceType = global::Android.Content.PM.ForegroundService.TypeConnectedDevice)]
    public class AndroidBackgroundService : Android.App.Service
    {
        private CancellationTokenSource _cts = new();
        // This is any integer value unique to the application.
        public const int SERVICE_RUNNING_NOTIFICATION_ID = 10000;
        private ILogger _logger = default!;
        private NetConnectConfig _netConfig = default!;
        private ILoggerFactory _loggerFactory = default!;
        private IRabbitRepo _rabbitRepo = default!;
        private IBackgroundService _backgroundService = default!;
        private IMonitorPingInfoView _monitorPingInfoView = default!;
        private LocalProcessorStates _processorStates = default!;
        private ICmdProcessorProvider _cmdProcessorProvider = default!;
        private IPlatformService _platformService = default!;
        private IFileRepo _fileRepo = default!;
        private IRootNamespaceProvider _rootProvider = default!;
        private NotificationManagerCompat _compatManager = default!;
        private int messageId = 0;
        private string _channelName = "FreeNetworkMonitor";
        private string _channelId = "fre_mon_channel";
        private string _channelDescription = "Quantum Network Monitor Agent notification channel";
        private bool _channelInitialized = false;
        private ILaunchHelper _launchHelper = default!;

        public const string ServiceBroadcastAction = "com.networkmonitor.service.STATUS";
        public const string ServiceStatusExtra = "ServiceStatus";
        public const string ServiceMessageExtra = "ServiceMessage";


        public AndroidBackgroundService()
        {
            _rootProvider = ServiceInitializer.RootProvider!;
        }

        public override IBinder? OnBind(Intent? intent)
        {
            return null;
        }

        public override void OnCreate()
        {
            base.OnCreate();
            _cts = new CancellationTokenSource();

            _logger = _rootProvider.ServiceProvider.GetRequiredService<ILogger<AndroidBackgroundService>>();
            _netConfig = _rootProvider.ServiceProvider.GetRequiredService<NetConnectConfig>();
            _loggerFactory = _rootProvider.ServiceProvider.GetRequiredService<ILoggerFactory>();
            _fileRepo = _rootProvider.ServiceProvider.GetRequiredService<IFileRepo>();
            _rabbitRepo = _rootProvider.ServiceProvider.GetRequiredService<IRabbitRepo>();
            _monitorPingInfoView = _rootProvider.ServiceProvider.GetRequiredService<IMonitorPingInfoView>();
            _processorStates = _rootProvider.ServiceProvider.GetRequiredService<LocalProcessorStates>();
            _cmdProcessorProvider = _rootProvider.ServiceProvider.GetRequiredService<ICmdProcessorProvider>();
            _platformService = _rootProvider.ServiceProvider.GetRequiredService<IPlatformService>();
            _launchHelper = _rootProvider.ServiceProvider.GetRequiredService<ILaunchHelper>();
        }
        private async Task StartAsync()
        {
            try
            {
                _backgroundService = new BackgroundService(_logger, _netConfig, _loggerFactory, _rabbitRepo, _fileRepo, _processorStates, _monitorPingInfoView, _cmdProcessorProvider,_launchHelper);
                var result = await _backgroundService.Start();
                _platformService.OnUpdateServiceState(result, true);

            }
            catch (Exception ex)
            {
                _logger.LogError($"Error initializing background service: {ex.Message}");
            }
        }

        private async Task StopAsync()
        {
             if (_backgroundService == null) return;
            try
            {
                var result = await _backgroundService.Stop();
                _platformService.OnUpdateServiceState(result, false);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error stopping background service: {ex.Message}");
            }
        }

        private PendingIntent GetViewAppPendingIntent()
        {
            var viewAppIntent = new Intent(this, _rootProvider.MainActivity);
            viewAppIntent.AddCategory(Intent.CategoryLauncher);
            // PendingIntent.GetActivity can return null, so check and throw if needed
            var pendingIntent = PendingIntent.GetActivity(this, 0, viewAppIntent, 0);
            if (pendingIntent == null)
                throw new InvalidOperationException("Failed to create PendingIntent.");
            return pendingIntent;
        }

        public override StartCommandResult OnStartCommand(Intent? intent, StartCommandFlags flags, int startId)
        {
            if (_cts.IsCancellationRequested)
            {
                _cts = new CancellationTokenSource();
            }
            string action = intent?.Action;
            if (action == "STOP_SERVICE")
            {
                try
                {
                    _logger.LogInformation($" SERVICE : stopping");
                    if ((int)Build.VERSION.SdkInt >= 24)
                    {
                        StopForeground(Android.App.StopForegroundFlags.Remove);
                    }
                    else
                    {
                        StopForeground(true);
                    }
                    _ = StopAsync();
                    _logger.LogInformation($" SERVICE : StartCommand Stop Completed");

                    return StartCommandResult.Sticky;
                }
                catch (Exception e)
                {
                    var result = new ResultObj() { Message = $" Error : Failed to Stop service . Error was : {e.Message}", Success = false };
                    _platformService.OnUpdateServiceState(result, false);
                    return StartCommandResult.Sticky;
                }
            }

            try
            {
                int logoId = _rootProvider.GetDrawable("logo");
                int viewId = _rootProvider.GetDrawable("view");
                _logger.LogInformation($" SERVICE : drawables {logoId} : {viewId}");
                if (!_channelInitialized)
                {
                    CreateNotificationChannel();
                }

                NotificationCompat.Builder builder = new NotificationCompat.Builder(this, _channelId)
                    .SetContentTitle("Network Monitor Agent")
                    .SetContentText("Service Running...")
                    .SetLargeIcon(BitmapFactory.DecodeResource(Platform.AppContext.Resources, logoId))
                    .SetSmallIcon(logoId)
                    .SetOngoing(true);

                Notification notification = builder.Build();
                _logger.LogInformation($" SERVICE : created notification");

                // Only call StartForeground with ForegroundService.TypeConnectedDevice if API >= 29
                if ((int)Build.VERSION.SdkInt >= 29)
                {
                    StartForeground(SERVICE_RUNNING_NOTIFICATION_ID, notification,
                        Android.Content.PM.ForegroundService.TypeConnectedDevice);
                }
                else
                {
                    StartForeground(SERVICE_RUNNING_NOTIFICATION_ID, notification);
                }

                _ = StartAsync();

            }
            catch (Exception e)
            {
                var result = new ResultObj() { Message = $" Error : Failed to Start service . Error was : {e.Message}", Success = false };
                result.Success = false;
                _platformService.OnUpdateServiceState(result, true);
            }
            _logger.LogInformation($" SERVICE : StartCommand Start completed");

            return StartCommandResult.Sticky;
        }

private void CreateNotificationChannel()
{
    // Only on API 26+ (Oreo)
    if ((int)Build.VERSION.SdkInt >= 26)
    {
        var channelNameJava = new Java.Lang.String(_channelName);
        var channel = new NotificationChannel(_channelId, channelNameJava, NotificationImportance.Default)
        {
            Description = _channelDescription
        };
        var managerObj = Platform.AppContext.GetSystemService(Context.NotificationService);
        if (managerObj is NotificationManager manager)
        {
            manager.CreateNotificationChannel(channel);
            _channelInitialized = true;
            _logger.LogInformation($" SERVICE : created notification channel.");
        }
    }
}

        public override void OnDestroy()
        {
            try
            {
                Task.Run(async () =>
                {
                    await StopAsync();
                }).Wait(TimeSpan.FromSeconds(5)); // Give it 5 seconds to complete
            }
            catch (Exception e)
            {
                _logger.LogError($" Error stopping service in OnDestroy: {e.Message}");
            }
            finally
            {
                base.OnDestroy();
            }
        }
    }
}
#endif