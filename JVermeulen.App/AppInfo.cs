using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;

namespace JVermeulen.App
{
    /// <summary>
    /// Static class for application info.
    /// </summary>
    public static class AppInfo
    {
        /// <summary>
        /// App name from the app domain.
        /// </summary>
        public static string Name => GetFriendlyNameFromDomain();

        /// <summary>
        /// App title from the assembly.
        /// </summary>
        public static string Title => GetTitleFromAssembly();

        /// <summary>
        /// App version from the assembly.
        /// </summary>
        public static string Version => GetVersionFromAssemly();

        /// <summary>
        /// App architecture (x86 or x64).
        /// </summary>
        public static string Architecture => Is64bit ? "x64" : "x86";

        /// <summary>
        /// App description (name, version and architecture).
        /// </summary>
        public static string Description => $"{Name} {Version} ({Architecture})";

        /// <summary>
        /// App guid from the assembly.
        /// </summary>
        public static Guid Guid => GetGuidFromAssembly();

        /// <summary>
        /// App runs as x64 architecture.
        /// </summary>
        public static bool Is64bit => IntPtr.Size == 8;

        /// <summary>
        /// App exe filename from the process.
        /// </summary>
        public static string FileName => GetFileNameFromCurrentProcess();

        /// <summary>
        /// App exe directory from the app domain.
        /// </summary>
        public static string DirectoryName => GetDirectoryFromCurrentDomain();

        /// <summary>
        /// App has a UI (Desktop or Console).
        /// </summary>
        public static bool HasUI => Environment.UserInteractive;

        /// <summary>
        /// App has a windows (Desktop).
        /// </summary>
        public static bool HasWindow => GetHasWindowFromCurrentProcess();

        /// <summary>
        /// App type (Desktop, Console or Service).
        /// </summary>
        public static string Type => HasUI ? (HasWindow ? "Desktop" : "Console") : "Service";

        /// <summary>
        /// OS name (e.g. Windows or Linux).
        /// </summary>
        public static string OSFriendlyName => GetOSFriendlyName();

        /// <summary>
        /// OS description (Type, version and architecture).
        /// </summary>
        public static string OSDescription => $"{RuntimeInformation.OSDescription} ({RuntimeInformation.OSArchitecture})";

        /// <summary>
        /// App current culture.
        /// </summary>
        public static string Culture => CultureInfo.CurrentCulture.DisplayName;

        /// <summary>
        /// App runs in DEBUG mode.
        /// </summary>
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
