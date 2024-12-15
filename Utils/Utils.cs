
namespace NetworkMonitor.Maui.Utils;

public class Utils {



public static async Task<bool> RequestStoragePermissionsAsync()
{
    var readStatus = await Permissions.CheckStatusAsync<Permissions.StorageRead>();
    var writeStatus = await Permissions.CheckStatusAsync<Permissions.StorageWrite>();

    if (readStatus != PermissionStatus.Granted)
    {
        readStatus = await Permissions.RequestAsync<Permissions.StorageRead>();
    }

    if (writeStatus != PermissionStatus.Granted)
    {
        writeStatus = await Permissions.RequestAsync<Permissions.StorageWrite>();
    }

    return readStatus == PermissionStatus.Granted && writeStatus == PermissionStatus.Granted;
}

}