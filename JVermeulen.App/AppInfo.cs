using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;

namespace JVermeulen.App
{
    public static class AppInfo
    {
        public static string Name => GetFriendlyNameFromDomain();
        public static string Title => GetTitleFromAssembly();
        public static string Version => GetVersionFromAssemly();
        public static string Architecture => Is64bit ? "x64" : "x86";
        public static string Description => $"{Name} {Version} ({Architecture})";

        public static Guid Guid => GetGuidFromAssembly();
        public static bool Is64bit => IntPtr.Size == 8;
        public static string FileName => GetFileNameFromCurrentProcess();
        public static string DirectoryName => GetDirectoryFromCurrentDomain();

        public static bool HasUI => Environment.UserInteractive;
        public static bool HasWindow => GetHasWindowFromCurrentProcess();
        public static string Type => HasUI ? (HasWindow ? "Desktop" : "Console") : "Service";
        
        public static string OSFriendlyName => GetOSFriendlyName();
        public static string OSDescription => $"{RuntimeInformation.OSDescription} ({RuntimeInformation.OSArchitecture})";

        public static string Culture => CultureInfo.CurrentCulture.DisplayName;
        public static bool IsDebug => GetIsDebug();

        private static string GetFriendlyNameFromDomain()
        {
            return AppDomain.CurrentDomain.FriendlyName;
        }

        private static string GetTitleFromAssembly()
        {
            return Assembly.GetEntryAssembly()
                           .GetCustomAttributes(typeof(AssemblyTitleAttribute))
                           .Select(a => a as AssemblyTitleAttribute)
                           .Select(a => a.Title)
                           .FirstOrDefault();
        }

        private static string GetVersionFromAssemly()
        {
            return Assembly.GetEntryAssembly()?
                           .GetName()?
                           .Version?
                           .ToString();
        }
        private static Guid GetGuidFromAssembly()
        {
            var assembly = Assembly.GetEntryAssembly();
            var attribute = assembly.GetCustomAttributes(typeof(GuidAttribute), true).Select(a => (GuidAttribute)a).FirstOrDefault();

            if (attribute != null)
                return new Guid(attribute.Value);
            else
                return default;
        }

        private static string GetFileNameFromCurrentProcess()
        {
            return Process.GetCurrentProcess().MainModule.FileName;
        }

        private static string GetDirectoryFromCurrentDomain()
        {
            return AppDomain.CurrentDomain.BaseDirectory;
        }

        private static bool GetHasWindowFromCurrentProcess()
        {
            return (Process.GetCurrentProcess().MainWindowHandle != IntPtr.Zero);
        }

        private static string GetOSFriendlyName()
        {
            var type = typeof(OSPlatform);
            var properties = type.GetProperties().Where(p => p.PropertyType == type);

            foreach (var property in properties)
            {
                var platform = (OSPlatform)property.GetValue(new OSPlatform());

                if (RuntimeInformation.IsOSPlatform(platform))
                    return platform.ToString();
            }

            return null;
        }

        private static bool GetIsDebug()
        {
#if DEBUG
            return true;
#else
            return false;
#endif

        }
    }
}
