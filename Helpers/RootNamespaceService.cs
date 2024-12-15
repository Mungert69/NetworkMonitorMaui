using Microsoft.Extensions.DependencyInjection;
using System;
using Microsoft.Maui.Controls;

namespace NetworkMonitor.Maui;

public class RootNamespaceService
{
    // Dynamically resolve MainActivity Type using reflection
    public static Type MainActivity
    {
        get
        {
            try
            {
#if ANDROID
                var appNamespace = Application.Current?.GetType().Namespace;
                if (!string.IsNullOrEmpty(appNamespace))
                {
                    var mainActivityType = Type.GetType($"{appNamespace}.MainActivity");
                    if (mainActivityType != null)
                        return mainActivityType;
                }
                throw new InvalidOperationException("Unable to resolve MainActivity. Ensure the namespace is correct.");
#else
                throw new PlatformNotSupportedException("MainActivity is only available on Android.");
#endif
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error resolving MainActivity: {ex.Message}");
                return null;
            }
        }
    }

  public static IServiceProvider ServiceProvider
{
    get
    {
        try
        {
            // Locate the MauiProgram type dynamically
            var appNamespace = Application.Current?.GetType().Namespace;
            Console.WriteLine($"Step 1: Detected Application Namespace: {appNamespace}");

            if (string.IsNullOrEmpty(appNamespace))
            {
                Console.WriteLine("Step 2: Failed to detect namespace. Application.Current or its type is null.");
                throw new InvalidOperationException("Application namespace could not be determined.");
            }

            // Search for the type in all loaded assemblies
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            Console.WriteLine($"Step 3: Loaded assemblies count: {assemblies.Length}");

            var mauiProgramType = assemblies
                .SelectMany(a =>
                {
                    try
                    {
                        return a.GetTypes();
                    }
                    catch (ReflectionTypeLoadException ex)
                    {
                        Console.WriteLine($"Step 3.1: Error loading types from assembly {a.FullName}: {ex.Message}");
                        return Array.Empty<Type>();
                    }
                })
                .FirstOrDefault(t => t.FullName == $"{appNamespace}.MauiProgram");

            if (mauiProgramType == null)
            {
                Console.WriteLine($"Step 4: MauiProgram type not found in namespace {appNamespace}. " +
                                  "Ensure the namespace is correct and the assembly is loaded.");
                throw new InvalidOperationException($"Unable to locate type '{appNamespace}.MauiProgram'.");
            }

            Console.WriteLine($"Step 5: MauiProgram type found: {mauiProgramType.FullName}");

            // Access the static ServiceProvider property
            var serviceProviderProperty = mauiProgramType.GetProperty(
                "ServiceProvider",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);

            if (serviceProviderProperty == null)
            {
                Console.WriteLine($"Step 6: ServiceProvider property not found on {mauiProgramType.FullName}. " +
                                  "Ensure it is public and static.");
                throw new InvalidOperationException("ServiceProvider property is not defined or is inaccessible.");
            }

            Console.WriteLine($"Step 7: ServiceProvider property found. Attempting to retrieve its value.");

            if (serviceProviderProperty.GetValue(null) is IServiceProvider serviceProvider)
            {
                Console.WriteLine($"Step 8: Successfully retrieved ServiceProvider.");
                return serviceProvider;
            }

            Console.WriteLine($"Step 9: ServiceProvider property value is null.");
            throw new InvalidOperationException("ServiceProvider is null. Ensure it is properly initialized.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error resolving ServiceProvider: {ex.Message}");
            Console.WriteLine($"Stack Trace: {ex.StackTrace}");
            return null;
        }
    }
}

   public static int GetDrawableResourceId(string resourceName, int defaultResourceId = 0)
{
#if ANDROID
    try
    {
        var context = Android.App.Application.Context;
        var resourceId = context.Resources.GetIdentifier(resourceName, "drawable", context.PackageName);
        
        if (resourceId == 0)
        {
            Console.WriteLine($"Resource '{resourceName}' not found. Returning default resource ID: {defaultResourceId}");
            return defaultResourceId; // Return the default if the resource is not found
        }

        return resourceId;
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error resolving drawable resource '{resourceName}': {ex.Message}. Returning default resource ID: {defaultResourceId}");
        return defaultResourceId; // Return the default if an error occurs
    }
#else
    throw new PlatformNotSupportedException("GetDrawableResourceId is only available on Android.");
#endif
}

    public static string GetAppDataDirectory()
    {
        return FileSystem.AppDataDirectory;
    }
}
