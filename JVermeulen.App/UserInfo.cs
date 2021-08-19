using System;
using System.Runtime.Versioning;
using System.Security.Principal;

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

        /// <summary>
        /// User has administrator rights (Windows only).
        /// </summary>
        [SupportedOSPlatform("windows")]
        public static bool IsAdmin => GetIsAdmin();

        [SupportedOSPlatform("windows")]
        private static bool GetIsAdmin()
        {
            using (WindowsIdentity identity = WindowsIdentity.GetCurrent())
            {
                return new WindowsPrincipal(identity).IsInRole(WindowsBuiltInRole.Administrator);
            }
        }
    }
}
