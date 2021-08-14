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
        public static string Name => GetNameFromDomain();
        public static string Title => GetTitleFromAssembly();
        public static string Version => GetVersionFromAssemly();
        public static string Architecture => $"{IntPtr.Size * 8}-bit";
        public static string Description => $"{Name} {Version} ({Architecture})";

        public static Guid Guid => GetGuidFromAssembly();
        public static bool Is64bit => IntPtr.Size == 8;
        public static string FileName => GetFileNameFromCurrentProcess();
        public static string DirectoryName => GetDirectoryFromCurrentDomain();
        public static bool? RunsSTA => GetSTAFromCurrentThread();

        public static bool HasUI => Environment.UserInteractive;
        public static string Culture => CultureInfo.CurrentCulture.DisplayName;
        public static bool IsDebug => GetIsDebug();

        private static string GetNameFromDomain()
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

        private static bool? GetSTAFromCurrentThread()
        {
            return Thread.CurrentThread.GetApartmentState() == ApartmentState.STA ? true : (Thread.CurrentThread.GetApartmentState() == ApartmentState.MTA ? false : null);
        }

        private static string GetDirectoryFromCurrentDomain()
        {
            return AppDomain.CurrentDomain.BaseDirectory;
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
