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
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
namespace NetworkMonitor.Maui.Services
{
    public interface IBackgroundService
    {
        Task<ResultObj> Start();
        Task<ResultObj> Stop();
        bool IsRunning { get; }
    }
    public class BackgroundService : IBackgroundService
    {
        // This is any integer value unique to the application.
        private ILogger _logger;
        private NetConnectConfig _netConfig;
        private ILoggerFactory _loggerFactory;
        private MonitorPingProcessor? _monitorPingProcessor;
        private IRabbitRepo _rabbitRepo;
        private IRabbitListener? _rabbitListener;
        private IFileRepo _fileRepo;
        private IMonitorPingInfoView _monitorPingInfoView;
        private LocalProcessorStates _processorStates;
        private ICmdProcessorProvider _cmdProcessorProvider;
        private IConnectProvider _connectProvider;
        private IBrowserHost? _browserHost;
        private IProtectedConfigManager _protectedConfigManager;
        private IAssetReadyService _assetReadyService;
        private bool _isRunning = false;
        private readonly SemaphoreSlim _lifecycleLock = new(1, 1);
        private CancellationTokenSource? _startupCts;
        public BackgroundService(ILogger logger, NetConnectConfig netConfig, ILoggerFactory loggerFactory, IRabbitRepo rabbitRepo, IFileRepo fileRepo, LocalProcessorStates processorStates, IMonitorPingInfoView monitorPingInfoView, ICmdProcessorProvider cmdProcessorProvider, IConnectProvider connectProvider, IBrowserHost browserHost, IProtectedConfigManager protectedConfigManager, IAssetReadyService assetReadyService)
        {
            _logger = logger;
            _netConfig = netConfig;
            _loggerFactory = loggerFactory;
            _rabbitRepo = rabbitRepo;
            _fileRepo = fileRepo;
            _monitorPingInfoView = monitorPingInfoView;
            _processorStates = processorStates;
            _cmdProcessorProvider = cmdProcessorProvider;
            _connectProvider = connectProvider;
            _browserHost = browserHost;
            _protectedConfigManager = protectedConfigManager;
            _assetReadyService = assetReadyService;

        }

        public bool IsRunning { get => _isRunning; }

        public async Task<ResultObj> Start()
        {
            await _lifecycleLock.WaitAsync();
            try
            {
                if (_isRunning)
                {
                    return new ResultObj
                    {
                        Success = true,
                        Message = " Background Service : Start : Agent is already running."
                    };
                }

                var result = new ResultObj();
                result.Message = " Background Service : Start : ";
                _startupCts?.Dispose();
                _startupCts = new CancellationTokenSource();
                var startupToken = _startupCts.Token;
                try
                {
                    await _assetReadyService.EnsureAssetsReadyAsync();
                    result = await _rabbitRepo.ConnectAndSetUp(startupToken, maxRetriesOverride: 3);
                    if (!result.Success)
                    {
                        _isRunning = false;
                        return result;
                    }
                    var resultCmdProcessorFactory = await _cmdProcessorProvider.Setup();
                    var resultConnectProvider = await _connectProvider.Setup();
                    var _connectFactory = new NetworkMonitor.Connection.ConnectFactory(_loggerFactory.CreateLogger<ConnectFactory>(), netConfig: _netConfig, _cmdProcessorProvider, _browserHost, _connectProvider);
#if ANDROID

#else
                    _ = _connectFactory.SetupChromium(_netConfig);
#endif
                    _monitorPingProcessor = new MonitorPingProcessor(_loggerFactory.CreateLogger<MonitorPingProcessor>(), _netConfig, _connectFactory, _fileRepo, _rabbitRepo, _processorStates, _protectedConfigManager, _monitorPingInfoView);
                    _rabbitListener = new RabbitListener(_monitorPingProcessor, _loggerFactory.CreateLogger<RabbitListener>(), _netConfig, _processorStates, _cmdProcessorProvider, _connectProvider);
                    var resultListener = await _rabbitListener.Setup(startupToken, maxRetriesOverride: 3);
                    var resultProcessor = await _monitorPingProcessor.Init(new ProcessorInitObj());
                    result.Message += resultCmdProcessorFactory.Message + resultConnectProvider.Message + resultListener.Message + resultProcessor.Message;
                    result.Success = resultCmdProcessorFactory.Success && resultConnectProvider.Success && resultProcessor.Success && resultListener.Success;
                    //result.Success = true;
                }
                catch (OperationCanceledException)
                {
                    result.Success = false;
                    result.Message += " Cancelled : background service startup was cancelled.";
                    _logger.LogInformation(result.Message);
                }
                catch (Exception e)
                {
                    result.Success = false;
                    result.Message += $" Error : failed to start background service . Error was : {e.Message}";
                    _logger.LogError(result.Message);
                }
                _isRunning = result.Success;
                return result;
            }
            finally
            {
                _lifecycleLock.Release();
            }
        }
        public async Task<ResultObj> Stop()
        {
            _startupCts?.Cancel();
            await _lifecycleLock.WaitAsync();
            try
            {
                if (!_isRunning && _rabbitListener == null && _monitorPingProcessor == null)
                {
                    return new ResultObj
                    {
                        Success = true,
                        Message = " Background Service : Stop : Agent is already stopped."
                    };
                }

                var result = new ResultObj();
                result.Message = " Background Service : Stop : ";
                result.Success = true;
                try
                {
                    _logger.LogInformation("Shutting down RabbitListener.");
                    if (_rabbitListener != null)
                    {
                        await _rabbitListener.Shutdown();
                        _rabbitListener = null;
                        result.Message += " Success : Shutdown RabbitListener.";
                    }
                    else
                    {
                        result.Message += " Nothing to do : RabbitListener was not running.";
                    }

                }
                catch (Exception e)
                {
                    _logger.LogError("Error during shutting down RabbitListener: " + e.ToString());
                    result.Success = false;
                }
                try
                {
                    _logger.LogInformation("Shutting down MonitorPingProcessor.");
                    if (_monitorPingProcessor != null)
                    {
                        await _monitorPingProcessor.OnStoppingAsync();
                        _monitorPingProcessor = null;
                    }
                    result.Message += " Success : Shutdown MonitorPingProcessor.";
                }
                catch (Exception e)
                {
                    _logger.LogError("Error during shutting down MonitorPingProcessor: " + e.ToString());
                    result.Success = false;
                }
                _isRunning = false;
                _startupCts?.Dispose();
                _startupCts = null;
                return result;
            }
            finally
            {
                _lifecycleLock.Release();
            }
        }
    }
}
