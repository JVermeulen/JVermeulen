using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Versioning;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;

namespace JVermeulen.App
{
    public static class UserInfo
    {
        public static string UserName => Environment.UserName;

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
