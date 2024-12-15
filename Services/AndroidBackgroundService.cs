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
        private ICmdProcessorProvider _cmdProcessorProvider ;
        private IPlatformService _platformService;

        private IFileRepo _fileRepo;
public const string ServiceBroadcastAction = "com.networkmonitor.service.STATUS";
public const string ServiceStatusExtra = "ServiceStatus";
public const string ServiceMessageExtra = "ServiceMessage";

        public AndroidBackgroundService()
        {

        }

        public override IBinder? OnBind(Intent? intent)
        {
            return null;
        }
        public override void OnCreate()
        {
            base.OnCreate();
            try {
                _cts = new CancellationTokenSource();

            _logger = RootNamespaceService.ServiceProvider.GetRequiredService<ILogger<AndroidBackgroundService>>();
            _netConfig = RootNamespaceService.ServiceProvider.GetRequiredService<NetConnectConfig>();
            _loggerFactory = RootNamespaceService.ServiceProvider.GetRequiredService<ILoggerFactory>();
            _fileRepo = RootNamespaceService.ServiceProvider.GetRequiredService<IFileRepo>();
            _rabbitRepo = RootNamespaceService.ServiceProvider.GetRequiredService<IRabbitRepo>();
            _monitorPingInfoView = RootNamespaceService.ServiceProvider.GetRequiredService<IMonitorPingInfoView>();
            _processorStates=RootNamespaceService.ServiceProvider.GetRequiredService<LocalProcessorStates>();
            _cmdProcessorProvider=RootNamespaceService.ServiceProvider.GetRequiredService<ICmdProcessorProvider>();
            _platformService= RootNamespaceService.ServiceProvider.GetRequiredService<IPlatformService>();
            _backgroundService = new BackgroundService(_logger, _netConfig, _loggerFactory, _rabbitRepo, _fileRepo,_processorStates, _monitorPingInfoView, _cmdProcessorProvider );
            _logger.LogInformation($" Success : got DI objects and injected into BackgroudService");
            }
            catch (Exception e){
                _logger.LogError($" Error : in OnCreate : {e.Message}");
                
            }
             
        }
        private async Task<ResultObj> StartAsync()
        {
            var result=new ResultObj();
            try
            {
                 _logger.LogInformation($" SERVICE : Starting AndroidBackgroundService");
           
                result = await _backgroundService.Start();
                _platformService.OnUpdateServiceState(result, _backgroundService.IsRunning);
               _logger.LogInformation($" SERVICE : Success : Started AndroidBackgroundService");
           

            }
            catch (Exception ex)
            {   
               result.Success=false;
               result.Message=$" Error starting  AndroidBackgroundService: {ex.Message}";
                 _logger.LogError(result.Message);
                  _platformService.OnUpdateServiceState(result, _backgroundService.IsRunning);
               
            }
            return result;
        }

        private async Task<ResultObj> StopAsync()
        {
              var result=new ResultObj();
            try
            {
                  _logger.LogInformation($" SERVICE : Stopping AndroidBackgroundService");
            result = await _backgroundService.Stop();
               _platformService.OnUpdateServiceState(result, _backgroundService.IsRunning);
                _logger.LogInformation($" SERVICE : Success : Stopped AndroidBackgroundService");
           
            }
            catch (Exception ex)
            {
            result.Success=false;
               result.Message=$"Error : stopping background service: {ex.Message}";
                 _logger.LogError(result.Message);
                  _platformService.OnUpdateServiceState(result, _backgroundService.IsRunning);
               
            }
            return result;
        }

        private PendingIntent? GetViewAppPendingIntent()
        {
            var viewAppIntent = new Intent(this,RootNamespaceService.MainActivity); // Replace 'MainActivity' with your main activity class
            viewAppIntent.SetAction(Intent.ActionMain);
            viewAppIntent.AddCategory(Intent.CategoryLauncher);
            return PendingIntent.GetActivity(this, 0, viewAppIntent, 0);
        }

        public override StartCommandResult OnStartCommand(Intent? intent, StartCommandFlags flags, int startId)
        {
             if (_cts.IsCancellationRequested)
            {
                _cts = new CancellationTokenSource();
            }
            try { 
                if (intent?.Action == "STOP_SERVICE") {

                Task.Run(async () =>
                    {
                        
                      
#pragma warning disable CA1422
                        StopForeground(true);
                    //StopBackgroundService(true);
#pragma warning restore CA1422

                        
                    var result= await StopAsync();
             
                       
                    }, _cts.Token);
                return StartCommandResult.Sticky;
                
                }
               
            }
            catch (Exception e){
                var result=new ResultObj(){Message=$" Error : Failed to Stop service . Error was : {e.Message}",Success=false};
            _platformService.OnUpdateServiceState(result, _backgroundService.IsRunning);
               return StartCommandResult.Sticky;
            }                          
            try
            {
                 Task.Run(async () =>
                {
                 _logger.LogInformation($" SERVICE : Getting Image Resources for AndroidBackgroundService");
           
                var logoResourceId = RootNamespaceService.GetDrawableResourceId("logo", Android.Resource.Drawable.IcDialogAlert); 
        var viewActionResourceId = RootNamespaceService.GetDrawableResourceId("view", Android.Resource.Drawable.IcMenuView); 
 var stopActionResourceId = RootNamespaceService.GetDrawableResourceId("stop", Android.Resource.Drawable.IcDelete); // Default stop icon

                if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
                {
#pragma warning disable CA1416, CA1422
                    Notification notification;
                    NotificationChannel channel = new NotificationChannel("channel_id", "Free Network Monitor Agent Agent", NotificationImportance.Low);
                    var notificationService=Context.NotificationService;
                    NotificationManager? notificationManager = (NotificationManager?)GetSystemService(notificationService);
                    notificationManager?.CreateNotificationChannel(channel);
                    /*var stopAction = new Notification.Action.Builder(
                            NetworkMonitorAgent.Resource.Drawable.stop,
                            "Stop",
                            GetStopServicePendingIntent())
                             .Build();

                    var viewAction = new Notification.Action.Builder(
                         NetworkMonitorAgent.Resource.Drawable.view,
                         "View",
                         GetViewAppPendingIntent())
                        .Build();
                        */
 _logger.LogInformation($" SERVICE : creating notification channel for AndroidBackgroundService");
           
                     notification = new Notification.Builder(this, "channel_id")
                        .SetAutoCancel(false)
                        .SetOngoing(true)
                        .SetContentTitle("Free Network Monitor Agent Agent")
                        .SetContentText("Monitoring network...")
                        .SetSmallIcon(logoResourceId)
                        .Build();

                         _logger.LogInformation($" SERVICE : starting in foreground service AndroidBackgroundService");
           
                    if (Build.VERSION.SdkInt < BuildVersionCodes.Tiramisu)
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
                    _logger.LogInformation($" SERVICE : creating notification channel for AndroidBackgroundService");
           
      
                    Notification notification = new NotificationCompat.Builder(this)
                                   .SetContentTitle("Free Network Monitor Agent Agent")
                                   .SetContentText("Monitoring network...")
                                   .SetSmallIcon(logoResourceId)
                                   .SetOngoing(true)
                                   //.AddAction(NetworkMonitorAgent.Resource.Drawable.stop, "Stop", GetStopServicePendingIntent())
                                   .AddAction(viewActionResourceId, "Open", GetViewAppPendingIntent()) // Ensure you have an icon for 'View App'
                                   .Build();
                     _logger.LogInformation($" SERVICE : starting in foreground service AndroidBackgroundService");
           
                    StartForeground(SERVICE_RUNNING_NOTIFICATION_ID, notification);
#pragma warning restore CS0618
                }
                                         _logger.LogInformation($" SERVICE : starting background service for AndroidBackgroundService");
           
               var result=await StartAsync();
               if (!result.Success) ;//TODO close the notification
                
                }, _cts.Token);
            }
           catch (Exception e){
             var result=new ResultObj(){Message=$" Error : Failed to Start service . Error was : {e.Message}",Success=false};
            _logger.LogError($" Error : failed to start AndroidBackgroundService. Error was : {e.Message}");
           
             _platformService.OnUpdateServiceState(result, _backgroundService.IsRunning);
            }
            return StartCommandResult.Sticky;
        }

       public override void OnDestroy()
{
    try
    {
        Task.Run(async () =>
        {
            await StopAsync();
        }).Wait(TimeSpan.FromSeconds(10)); // Give it 5 seconds to complete
    }
    catch (Exception e)
    {
        _logger.LogError($" Error : stopping service in OnDestroy. Error was : {e.Message}");
    }
    finally
    {
        base.OnDestroy();
    }
}
    }
}
#endif