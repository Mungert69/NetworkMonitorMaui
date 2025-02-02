using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NetworkMonitor.Connection;
using NetworkMonitor.DTOs;
using NetworkMonitor.Objects.Repository;
using NetworkMonitor.Processor.Services;
using NetworkMonitor.Api.Services;
using NetworkMonitor.Objects.ServiceMessage;
using NetworkMonitor.Objects;
using NetworkMonitor.Maui.ViewModels;
using CommunityToolkit.Maui;
using Microsoft.Maui.Hosting;
using Microsoft.Maui.Storage;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.IO;
using System.Text;

namespace NetworkMonitor.Maui;
public class CopyAssetsHelper
{


private static async Task<string> CopyAssetType(string assetType, bool setPerms, string directoryName, string[] assetFiles, string localPath)
{
    var outputStr = new StringBuilder();
    var copyTasks = assetFiles.Select(async assetFile =>
    {
        string assetFilePath = Path.Combine(directoryName, assetFile);
        string localFilePath = Path.Combine(localPath, assetFile);

        try
        {
            if (File.Exists(localFilePath))
            {
                // Compare file size before copying
                using var stream = await FileSystem.OpenAppPackageFileAsync(assetFilePath);
                if (new FileInfo(localFilePath).Length == stream.Length)
                {
                    return $"Skipped {assetType} file: {assetFile} (Already up-to-date)";
                }
            }

            // Ensure the directory exists
            Directory.CreateDirectory(Path.GetDirectoryName(localFilePath)!);

            // Copy file
            using var sourceStream = await FileSystem.OpenAppPackageFileAsync(assetFilePath);
            using var targetStream = new FileStream(localFilePath, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: 81920, useAsync: true);
            await sourceStream.CopyToAsync(targetStream);

            if (setPerms && IsBinary(assetFile, out string binaryType))
            {
                SetExecutablePermission(localFilePath);
                return $"Permission set for {binaryType}: {localFilePath}";
            }

            return $"Copied {assetType} file: {assetFile}";
        }
        catch (Exception e)
        {
            return $"Error copying {assetType} file {assetFile}: {e.Message}";
        }
    });

    var results = await Task.WhenAll(copyTasks);
    foreach (var result in results.Where(r => r != null))
    {
        outputStr.AppendLine(result);
    }

    return outputStr.ToString();
}
    public static async Task<string> CopyAssetsToLocalStorage(string assetDirectoryName, string csAssetDirectoryName, string dllAssetDirectoryName)
        {
            var outputStr = new StringBuilder();

            try
            {

                outputStr.AppendLine($"------BEGIN------");

                outputStr.AppendLine($"Starting asset copy from : {assetDirectoryName}");

                var (assetFiles, listOutput) = await ListAssetFiles(assetDirectoryName);
                outputStr.Append(listOutput);
                string assetDir = "openssl";
                string localPath = Path.Combine(FileSystem.AppDataDirectory, assetDir);
                Directory.CreateDirectory(localPath);

                outputStr.Append(await CopyAssetType("asset", true, assetDirectoryName, assetFiles, localPath));


                outputStr.AppendLine($"Directory copied to: {localPath}");
                outputStr.Append(ListCopiedFiles(assetDir));

                outputStr.AppendLine($"Starting cs-asset copy from : {csAssetDirectoryName}");

                string csAssetDir = Path.Combine("openssl", "bin");
                var (csAssetFiles, csListOutput) = await ListAssetFiles(csAssetDirectoryName);
                outputStr.Append(csListOutput);
                string csLocalPath = Path.Combine(FileSystem.AppDataDirectory, csAssetDir);
                Directory.CreateDirectory(csLocalPath);
                outputStr.Append(await CopyAssetType("cs-asset", false, csAssetDirectoryName, csAssetFiles, csLocalPath));
                outputStr.AppendLine($"Directory copied to: {csLocalPath}");
                outputStr.Append(ListCopiedFiles(csAssetDir));

#if WINDOWS
            dllAssetDirectoryName = "windows" + dllAssetDirectoryName;
#endif

            outputStr.AppendLine($"Starting dll copy from : {dllAssetDirectoryName}");
            string dllAssetDir = Path.Combine("openssl", "bin", "dlls");
                var (dllAssetFiles, dllListOutput) = await ListAssetFiles(dllAssetDirectoryName);
                outputStr.Append(dllListOutput);
                string dllLocalPath = Path.Combine(FileSystem.AppDataDirectory, dllAssetDir);
                Directory.CreateDirectory(dllLocalPath);
                outputStr.Append(await CopyAssetType("dll", false, dllAssetDirectoryName, dllAssetFiles, dllLocalPath));
                outputStr.AppendLine($"Directory copied to: {dllLocalPath}");
                outputStr.Append(ListCopiedFiles(dllAssetDir));

                outputStr.Append(SetLDLibraryPath(Path.Combine(localPath, "lib64")));
            }
            catch (Exception ex)
            {
                outputStr.AppendLine($"Error copying assets: {ex.Message}");
            }
            return outputStr.ToString();
        }

        private static bool IsBinary(string assetFile, out string binaryType)
        {
            binaryType = "";

#if WINDOWS
            string trimmedPath = assetFile.Trim('/', '\\'); // Ensure path is sanitized for comparisons

            var binaries = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        { "/openssl.exe", "Windows OpenSSL" },
        { "/busybox.exe", "Windows Busybox" },
        { "/nmap.exe", "Windows Nmap" },
        { "/curl.exe", "Windows Curl" }
    };
#else
            string trimmedPath = assetFile.Trim('/', '\\'); // Ensure path is sanitized for comparisons

            var binaries = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        { "/openssl", "OpenSSL" },
        { "/busybox", "Busybox" },
        { "/nmap", "Nmap" },
        { "/curl", "Curl" },
    };
#endif

            foreach (var binary in binaries)
            {
                if (assetFile.EndsWith(binary.Key, StringComparison.OrdinalIgnoreCase))
                {
                    binaryType = binary.Value;
                    return true;
                }
            }

            return false;
        }
        private static string SetExecutablePermission(string filePath)
        {
            var outputStr = new StringBuilder();
            try
            {
                outputStr.AppendLine($"Attempting to set executable permission for: {filePath}");

#if ANDROID
        PermissionsHelper.MakeFileExecutable(filePath);
#elif WINDOWS

                outputStr.Append(ProcessFileForSymbolicLink(filePath));
#else
                // Existing implementation for other platforms
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "sh",
                        Arguments = $"-c \"chmod +x {filePath}\"",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };

                process.Start();

                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();

                process.WaitForExit();

                if (!string.IsNullOrEmpty(output))
                {
                    outputStr.AppendLine($"chmod output: {output}");
                }
                if (!string.IsNullOrEmpty(error))
                {
                    outputStr.AppendLine($"chmod error: {error}");
                }

                outputStr.AppendLine($"Set executable permission for: {filePath}");
#endif
            }
            catch (Exception ex)
            {
                outputStr.AppendLine($"Failed to set executable permission for {filePath}: {ex.Message}");
            }
            return outputStr.ToString();
        }

#if WINDOWS
        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern bool CreateSymbolicLink(
            string lpSymlinkFileName,
            string lpTargetFileName,
            int dwFlags);

        const int SYMBOLIC_LINK_FLAG_DIRECTORY = 0x1;

        private static void CreateSymbolicLinkWindows(string symlinkPath, string targetPath, bool isDirectory)
        {
            bool result = CreateSymbolicLink(symlinkPath, targetPath, isDirectory ? SYMBOLIC_LINK_FLAG_DIRECTORY : 0);
            if (!result)
            {
                throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
            }
        }
        private static string ProcessFileForSymbolicLink(string filePath)
        {
            // Check if the file path ends with .exe
            if (filePath.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
            {
                // Remove the .exe extension
                string targetPath = Path.ChangeExtension(filePath, null);

                // Create a symbolic link
                CreateSymbolicLinkWindows(filePath, targetPath, false);
                return "";
            }
            else
            {
                // Handle cases where the file does not end with .exe if needed
                return $"File does not end with .exe: {filePath}";
            }
        }
#endif


        private static string SetLDLibraryPath(string libraryPath)
        {
            var outputStr = new StringBuilder();
            try
            {
                string ldLibraryPath = $"LD_LIBRARY_PATH={libraryPath}";
                Environment.SetEnvironmentVariable("LD_LIBRARY_PATH", libraryPath);
                outputStr.AppendLine($"Set LD_LIBRARY_PATH to: {libraryPath}");
            }
            catch (Exception ex)
            {
                outputStr.AppendLine($"Failed to set LD_LIBRARY_PATH: {ex.Message}");
            }
            return outputStr.ToString();
        }

       
        private static async Task<(string[], string)> ListAssetFiles(string directoryName)
{
    var outputStr = new StringBuilder();
    try
    {
        string manifestFileName = Path.Combine(directoryName, "assets_manifest.txt");
        using var stream = await FileSystem.OpenAppPackageFileAsync(manifestFileName);
        using var reader = new StreamReader(stream);
        
        // Read all lines in one go
        var lines = (await reader.ReadToEndAsync())
                    .Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(line => line.TrimStart('.', '/'))
                    .ToArray();

        outputStr.AppendLine($"Success: assets from {directoryName} read successfully");
        foreach (var line in lines)
        {
            outputStr.AppendLine($"Preparing file: {line}");
        }

        return (lines, outputStr.ToString());
    }
    catch (Exception ex)
    {
        outputStr.AppendLine($"Error: reading asset manifest. Error was: {ex.Message}");
        return (Array.Empty<string>(), outputStr.ToString());
    }
}


        private static string ListCopiedFiles(string assetDir)
        {

            var outputStr = new StringBuilder(); try
            {
                string localPath = Path.Combine(FileSystem.AppDataDirectory, assetDir);
                string[] files = Directory.GetFiles(localPath, "*", SearchOption.AllDirectories);

                foreach (var file in files)
                {
                    outputStr.AppendLine($"File in local storage: {file}");
                }
            }
            catch (Exception ex)
            {
                outputStr.AppendLine($" Error : listing copied files. Error was :{ex.Message}");
            }
            return outputStr.ToString();
        }

    }
