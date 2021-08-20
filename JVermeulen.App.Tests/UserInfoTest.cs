using System;
using Xunit;

namespace JVermeulen.App.Tests
{
    public class UserInfoTest
    {
        [Fact]
        public void NameNotDefault()
        {
            UserInfo.Name.AssertNotDefault("UserInfo.Name");
        }

        [Fact]
        public void IsWindowsAdminNoException()
        {
            var value = UserInfo.IsWindowsAdmin;
        }
    }
}
