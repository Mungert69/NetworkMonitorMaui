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
using NetworkMonitor.Security;
using Microsoft.Extensions.Configuration;
using NetworkMonitor.Maui.Helpers;
using NetworkMonitor.Maui;


namespace NetworkMonitor.Maui.Services
{

    [Android.App.Service(ForegroundServiceType = global::Android.Content.PM.ForegroundService.TypeConnectedDevice)]
    public class AndroidBackgroundService : Android.App.Service
    {
        private CancellationTokenSource _cts = new();
        // This is any integer value unique to the application.
        public const int SERVICE_RUNNING_NOTIFICATION_ID = 10000;
        private ILogger _logger;
        private NetConnectConfig _netConfig;
        private ILoggerFactory _loggerFactory;
        private IRabbitRepo _rabbitRepo;
        private IBackgroundService? _backgroundService;
        private IMonitorPingInfoView _monitorPingInfoView;
        private LocalProcessorStates _processorStates;
        private ICmdProcessorProvider _cmdProcessorProvider;
        private IConnectProvider _connectProvider;
        private IPlatformService _platformService;
        private IFileRepo _fileRepo;
        private IRootNamespaceProvider _rootProvider;
        private IProtectedConfigManager _protectedConfigManager;
        private IAssetReadyService _assetReadyService;
        private int messageId = 0;
        private string _channelName = "FreeNetworkMonitor";
        private string _channelId = "fre_mon_channel";
        private string _channelDescription = "Quantum Network Monitor Agent notification channel";
        private bool _channelInitialized = false;
        private IBrowserHost _browserHost;
        private readonly object _lifecycleLock = new();
        private Task<ResultObj>? _startTask;
        private Task<ResultObj>? _stopTask;
        private AndroidMemoryTelemetryHelper? _memoryTelemetry;

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
            _connectProvider = _rootProvider.ServiceProvider.GetRequiredService<IConnectProvider>();
            _platformService = _rootProvider.ServiceProvider.GetRequiredService<IPlatformService>();
            _browserHost = _rootProvider.ServiceProvider.GetRequiredService<IBrowserHost>();
            _protectedConfigManager = _rootProvider.ServiceProvider.GetRequiredService<IProtectedConfigManager>();
            _assetReadyService = _rootProvider.ServiceProvider.GetRequiredService<IAssetReadyService>();
            _memoryTelemetry ??= new AndroidMemoryTelemetryHelper(_logger, TimeSpan.FromSeconds(30));
            _memoryTelemetry.Start();
        }
        private async Task StartAsync()
        {
            Task<ResultObj> startTask;
            try
            {
                lock (_lifecycleLock)
                {
                    if (_backgroundService?.IsRunning == true)
                    {
                        var alreadyRunning = new ResultObj
                        {
                            Success = true,
                            Message = " Android Background Service : Start : Agent is already running."
                        };
                        _platformService.OnUpdateServiceState(alreadyRunning, true);
                        return;
                    }

                    if (_startTask == null || _startTask.IsCompleted)
                    {
                        _backgroundService ??= new BackgroundService(_logger, _netConfig, _loggerFactory, _rabbitRepo, _fileRepo, _processorStates, _monitorPingInfoView, _cmdProcessorProvider, _connectProvider, _browserHost, _protectedConfigManager, _assetReadyService);
                        _startTask = _backgroundService.Start();
                    }
                    startTask = _startTask;
                }

                var result = await startTask;
                _platformService.OnUpdateServiceState(result, true);

            }
            catch (Exception ex)
            {
                _logger.LogError($"Error initializing background service: {ex.Message}");
            }
        }

        private async Task StopAsync()
        {
            Task<ResultObj> stopTask;
            try
            {
                lock (_lifecycleLock)
                {
                    if (_backgroundService == null)
                    {
                        var alreadyStopped = new ResultObj
                        {
                            Success = true,
                            Message = " Android Background Service : Stop : Agent is already stopped."
                        };
                        _platformService.OnUpdateServiceState(alreadyStopped, false);
                        StopSelf();
                        return;
                    }

                    if (_stopTask == null || _stopTask.IsCompleted)
                    {
                        _stopTask = _backgroundService.Stop();
                    }
                    stopTask = _stopTask;
                }

                var result = await stopTask;
                if (result.Success)
                {
                    lock (_lifecycleLock)
                    {
                        _backgroundService = null;
                        _startTask = null;
                        _stopTask = null;
                    }
                    StopSelf();
                }
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

                    return StartCommandResult.NotSticky;
                }
                catch (Exception e)
                {
                    var result = new ResultObj() { Message = $" Error : Failed to Stop service . Error was : {e.Message}", Success = false };
                    _platformService.OnUpdateServiceState(result, false);
                    return StartCommandResult.NotSticky;
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
                _memoryTelemetry?.Stop();
                _memoryTelemetry?.Dispose();
                _memoryTelemetry = null;
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
