using JVermeulen.App;
using System;
using Xunit;

namespace JVermeulen.App.Tests
{
    public class AppInfoTest
    {
        [Fact]
        public void Run()
        {
            IsNotDefault("Name", AppInfo.Name);
            IsNotDefault("Title", AppInfo.Title);
            IsNotDefault("Version", AppInfo.Version);
            IsNotDefault("Architecture", AppInfo.Architecture);
            IsNotDefault("Description", AppInfo.Description);
            IsNotDefault("Guid", AppInfo.Guid);
        }

        private void IsNotDefault(string name, object value)
        {
            var result = value != default;

            Assert.True(result, $"{name} should have a value.");
        }
    }
}
