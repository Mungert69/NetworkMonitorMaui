// Platforms/Android/PermissionsHelper.cs
using Android.Util;
using Java.IO;
using System;

namespace NetworkMonitor.Maui
{
    public static class PermissionsHelper
    {
        public static void MakeFileExecutable(string filePath)
        {
            try
            {
                var file = new Java.IO.File(filePath);
                if (!file.CanExecute())
                {
                    Log.Debug("PermissionsHelper", $"File {filePath} is not executable, trying to make it executable...");
                    if (file.SetExecutable(true))
                    {
                        Log.Debug("PermissionsHelper", $"File {filePath} is now executable.");
                    }
                    else
                    {
                        Log.Debug("PermissionsHelper", $"Failed to make the file {filePath} executable.");
                    }
                }
                else
                {
                    Log.Debug("PermissionsHelper", $"File {filePath} is already executable.");
                }
            }
            catch (Exception ex)
            {
                Log.Debug("PermissionsHelper", $"Error while setting executable permission for {filePath}: {ex.Message}");
            }
        }
    }
}
