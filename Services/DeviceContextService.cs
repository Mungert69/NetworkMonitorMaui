using Microsoft.Extensions.Logging;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Devices.Sensors;
using NetworkMonitor.Connection;
using NetworkMonitor.Security;
using NetworkMonitor.Utils.Helpers;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace NetworkMonitor.Maui.Services
{
    public interface IDeviceContextService
    {
        Task RefreshAndPersistAsync();
    }

    public sealed class DeviceContextService : IDeviceContextService
    {
        private readonly ILogger _logger;
        private readonly NetConnectConfig _netConfig;
        private readonly IProtectedConfigManager _protectedConfigManager;

        public DeviceContextService(
            ILogger<DeviceContextService> logger,
            NetConnectConfig netConfig,
            IProtectedConfigManager protectedConfigManager)
        {
            _logger = logger;
            _netConfig = netConfig;
            _protectedConfigManager = protectedConfigManager;
        }

        public async Task RefreshAndPersistAsync()
        {
            try
            {
                var context = DeviceContextHelper.CaptureNetworkContext(_logger);
                context.CaptureSource = "maui";

                await TryEnrichWithGpsAsync(context).ConfigureAwait(false);

                _netConfig.DeviceContext = context;
                _netConfig.MonitorLocation = DeviceContextHelper.BuildMonitorLocation(context, _netConfig.MonitorLocation);
                await _protectedConfigManager
                    .SaveConfigurationAsync(_netConfig, ProtectedConfigurationParameters.All)
                    .ConfigureAwait(false);

                _logger.LogInformation("Refreshed and persisted device context. monitor_location={Location}", _netConfig.MonitorLocation);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to refresh device context.");
            }
        }

        private async Task TryEnrichWithGpsAsync(DeviceContext context)
        {
            try
            {
                if (!Geolocation.Default.IsSupported)
                {
                    context.HasGps = false;
                    return;
                }

                var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>().ConfigureAwait(false);
                if (status != PermissionStatus.Granted)
                {
                    context.HasGps = false;
                    return;
                }

                var location = await Geolocation.Default.GetLastKnownLocationAsync().ConfigureAwait(false);
                if (location == null)
                {
                    location = await Geolocation.Default.GetLocationAsync(
                        new GeolocationRequest(GeolocationAccuracy.Medium, TimeSpan.FromSeconds(8)))
                        .ConfigureAwait(false);
                }

                if (location == null)
                {
                    context.HasGps = false;
                    return;
                }

                context.HasGps = true;
                context.Geo.Latitude = location.Latitude;
                context.Geo.Longitude = location.Longitude;
                context.Geo.AccuracyMeters = location.Accuracy;
                context.Geo.Source = "maui-geolocation";

                try
                {
                    var placemarks = await Geocoding.Default
                        .GetPlacemarksAsync(location.Latitude, location.Longitude)
                        .ConfigureAwait(false);
                    var place = placemarks?.FirstOrDefault();
                    if (place != null)
                    {
                        context.NearestTown = place.Locality ?? place.SubAdminArea ?? place.AdminArea ?? string.Empty;
                        context.Country = place.CountryName ?? string.Empty;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "MAUI reverse geocode failed; trying network reverse geocode.");
                }

                if (string.IsNullOrWhiteSpace(context.NearestTown))
                {
                    await DeviceContextHelper
                        .EnrichWithReverseGeocodeAsync(context, location.Latitude, location.Longitude, _logger)
                        .ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                context.HasGps = false;
                _logger.LogDebug(ex, "GPS enrichment failed.");
            }
        }
    }
}
