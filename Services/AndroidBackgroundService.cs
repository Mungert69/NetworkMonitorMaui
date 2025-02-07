#if ANDROID
using Android.App;
using Android.Content;
using Android.OS;
using Microsoft.Extensions.Logging;
using NetworkMonitor.Connection;
using NetworkMonitor.Processor.Services;
using NetworkMonitor.Objects.Repository;
using NetworkMonitor.Objects.ServiceMessage;
using NetworkMonitor.Utils.Helpers;
using NetworkMonitor.DTOs;
using NetworkMonitor.Objects;
using Microsoft.Extensions.Configuration;
using AndroidX.Core.App;
using NetworkMonitor.Maui.Services;
using NetworkMonitor.Maui;


namespace NetworkMonitor.Maui.Services
{

    [Android.App.Service(ForegroundServiceType = global::Android.Content.PM.ForegroundService.TypeConnectedDevice)]
    public class AndroidBackgroundService : Android.App.Service
    {
        private CancellationTokenSource _cts;
        // This is any integer value unique to the application.
        public const int SERVICE_RUNNING_NOTIFICATION_ID = 10000;
        private ILogger _logger;
        private NetConnectConfig _netConfig;
        private ILoggerFactory _loggerFactory;
        private IRabbitRepo _rabbitRepo;
        private IBackgroundService _backgroundService;
        private IMonitorPingInfoView _monitorPingInfoView;
        private LocalProcessorStates _processorStates;
        //private ILocalCmdProcessorStates _scanProcessorStates;
        private ICmdProcessorProvider _cmdProcessorProvider;

        private IPlatformService _platformService;

        private IFileRepo _fileRepo;
        private IRootNamespaceProvider _rootProvider;
        public const string ServiceBroadcastAction = "com.networkmonitor.service.STATUS";
        public const string ServiceStatusExtra = "ServiceStatus";
        public const string ServiceMessageExtra = "ServiceMessage";

        public AndroidBackgroundService()
        {
            _rootProvider = ServiceInitializer.RootProvider;

        }

        public override IBinder OnBind(Intent intent)
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
              
        }
        private async Task StartAsync()
        {
            try
            {
                _backgroundService = new BackgroundService(_logger, _netConfig, _loggerFactory, _rabbitRepo, _fileRepo, _processorStates, _monitorPingInfoView, _cmdProcessorProvider);
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
            return PendingIntent.GetActivity(this, 0, viewAppIntent, 0);
        }

        public override StartCommandResult OnStartCommand(Intent intent, StartCommandFlags flags, int startId)
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

                    Task.Run(async () =>
                        {
#pragma warning disable CA1422
                            StopForeground(true);
                            //StopBackgroundService(true);
#pragma warning restore CA1422
                            await StopAsync();
                        }, _cts.Token);
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
        Task.Run(async () =>
        {
            try
            {
                int logoId = _rootProvider.GetDrawable("logo");
                int viewId = _rootProvider.GetDrawable("view");
                _logger.LogInformation($" SERVICE : drawables {logoId} : {viewId}");
                if (OperatingSystem.IsAndroidVersionAtLeast((int)BuildVersionCodes.O))
                   {
#pragma warning disable CA1416, CA1422
                       Notification notification;
                       NotificationChannel channel = new NotificationChannel("channel_id", "Free Network Monitor Agent", NotificationImportance.Low);
                       channel.Description = "Network monitoring service";
                        channel.SetSound(null, null); // Optional: Disable sound
                        channel.EnableVibration(false); // Optional: Disable vibration
                       NotificationManager notificationManager = (NotificationManager)GetSystemService(Context.NotificationService);
                       notificationManager.CreateNotificationChannel(channel);
                       /*var stopAction = new Notification.Action.Builder(
                                _rootProvider.GetDrawable("stop"),
                               "Stop",
                               GetStopServicePendingIntent())
                                .Build();

                       var viewAction = new Notification.Action.Builder(
                             _rootProvider.GetDrawable("view"),
                            "View",
                            GetViewAppPendingIntent())
                           .Build();
                           */
                       _logger.LogInformation($" SERVICE : created notification channel.");

                       notification = new Notification.Builder(this, "channel_id")
                           .SetAutoCancel(false)
                           .SetOngoing(true)
                           .SetContentTitle("Free Network Monitor Agent")
                           .SetContentText("Monitoring network...")
                           .SetSmallIcon( _rootProvider.GetDrawable("logo"))
                           .Build();

                       _logger.LogInformation($" SERVICE : cratetd notification");

                      if (!OperatingSystem.IsAndroidVersionAtLeast((int)BuildVersionCodes.Tiramisu))
                       {
                           StartForeground(SERVICE_RUNNING_NOTIFICATION_ID, notification);
                       }
                       else
                       {
                           StartForeground(SERVICE_RUNNING_NOTIFICATION_ID, notification,
                            Android.Content.PM.ForegroundService.TypeConnectedDevice);
                       }
#pragma warning restore CA1416, CA1422
                   }
                   else
                   {
#pragma warning disable CS0618
                       // For API below 26
                       Notification notification = new NotificationCompat.Builder(this)
                                       .SetContentTitle("Free Network Monitor Agent")
                                       .SetContentText("Monitoring network...")
                                       .SetSmallIcon( _rootProvider.GetDrawable("logo"))
                                       .SetOngoing(true)
                                       .AddAction( _rootProvider.GetDrawable("view"), "Open", GetViewAppPendingIntent()) // Ensure you have an icon for 'View App'
                                       .Build();
                       StartForeground(SERVICE_RUNNING_NOTIFICATION_ID, notification);
#pragma warning restore CS0618
                   }
                    await StartAsync();
                   
            }
            catch (Exception e)
            {
                var result = new ResultObj() { Message = $" Error : Failed to Start service . Error was : {e.Message}", Success = false };
                _platformService.OnUpdateServiceState(result, true);
            }
            _logger.LogInformation($" SERVICE : StartCommand Start completed");
        }, _cts.Token);
            return StartCommandResult.Sticky;
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