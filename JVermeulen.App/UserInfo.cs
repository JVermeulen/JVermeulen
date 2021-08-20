using System;

namespace JVermeulen.App
{
    /// <summary>
    /// Static class for user info.
    /// </summary>
    public static class UserInfo
    {
        /// <summary>
        /// User name.
        /// </summary>
        public static string Name => Environment.UserName;

        ///// <summary>
        ///// User has administrator rights (Windows only).
        ///// </summary>
        //public static bool IsWindowsAdmin => GetIsWindowsAdmin();

        //[System.Diagnostics.CodeAnalysis.SuppressMessage("Interoperability", "CA1416:Validate platform compatibility", Justification = "Returns false when not on Windows.")]
        //private static bool GetIsWindowsAdmin()
        //{
        //    if (!AppInfo.OSIsWindows)
        //        return false;

        //    using (WindowsIdentity identity = WindowsIdentity.GetCurrent())
        //    {
        //        return new WindowsPrincipal(identity).IsInRole(WindowsBuiltInRole.Administrator);
        //    }
        //}
    }
}
