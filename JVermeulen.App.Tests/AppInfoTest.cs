using System;
using Xunit;

namespace JVermeulen.App.Tests
{
    public class AppInfoTest
    {
        [Fact]
        public void NameNotDefault()
        {
            AppInfo.Name.AssertNotDefault("AppInfo.Name");
        }

        [Fact]
        public void TitleNotDefault()
        {
            AppInfo.Title.AssertNotDefault("AppInfo.Title");
        }

        [Fact]
        public void VersionNotDefault()
        {
            AppInfo.Version.AssertNotDefault("AppInfo.Version");
        }

        [Fact]
        public void ArchitectureNotDefault()
        {
            AppInfo.Architecture.AssertNotDefault("AppInfo.Architecture");
        }

        [Fact]
        public void DescriptionNotDefault()
        {
            AppInfo.Description.AssertNotDefault("AppInfo.Description");
        }

        [Fact]
        public void GuidNotDefault()
        {
            AppInfo.Guid.AssertNotDefault("AppInfo.Guid");
        }

        [Fact]
        public void Is64bitNoException()
        {
            var value = AppInfo.OSIsWindows;
        }

        [Fact]
        public void FileNameNotDefault()
        {
            AppInfo.FileName.AssertNotDefault("AppInfo.FileName");
        }

        [Fact]
        public void DirectoryNameNotDefault()
        {
            AppInfo.DirectoryName.AssertNotDefault("AppInfo.DirectoryName");
        }

        [Fact]
        public void HasUINoException()
        {
            var value = AppInfo.HasUI;
        }

        [Fact]
        public void HasWindowNoException()
        {
            var value = AppInfo.HasWindow;
        }

        [Fact]
        public void TypeNotDefault()
        {
            AppInfo.Type.AssertNotDefault("AppInfo.Type");
        }

        [Fact]
        public void OSFriendlyNameNotDefault()
        {
            AppInfo.OSFriendlyName.AssertNotDefault("AppInfo.OSFriendlyName");
        }

        [Fact]
        public void OSDescriptionNotDefault()
        {
            AppInfo.OSDescription.AssertNotDefault("AppInfo.OSDescription");
        }

        [Fact]
        public void OSIsWindowsNoException()
        {
            var value = AppInfo.OSIsWindows;
        }

        [Fact]
        public void CultureNotDefault()
        {
            AppInfo.Culture.AssertNotDefault("AppInfo.Culture");
        }

        [Fact]
        public void IsDebugNoException()
        {
            var value = AppInfo.IsDebug;
        }
    }
}
