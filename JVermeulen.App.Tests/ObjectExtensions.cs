using System;
using Xunit;

namespace JVermeulen.App.Tests
{
    public static class ObjectExtensions
    {
        public static void AssertNotDefault(this object value, string name)
        {
            var result = value != default;

            Assert.True(result, $"{name} should have a value.");
        }
    }
}
