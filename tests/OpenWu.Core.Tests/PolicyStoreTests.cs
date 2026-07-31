using System;
using System.IO;
using OpenWu.Core.Policy;
using Xunit;

namespace OpenWu.Core.Tests;

public sealed class PolicyStoreTests
{
    [Fact]
    public void Load_ReturnsDefaults_WhenFileDoesNotExist()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"openwu_test_{Guid.NewGuid()}.json");
        try
        {
            var store = new PolicyStore(tempFile);
            var policy = store.Load();

            Assert.NotNull(policy);
            Assert.Equal(1, policy.SchemaVersion);
            Assert.Equal("MicrosoftUpdate", policy.Service);
            Assert.False(policy.IncludeDrivers);
            Assert.Contains("Preview", policy.DenyTitlesContains);
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }

    [Fact]
    public void SaveAndLoad_RoundTripsPolicyCorrectly()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"openwu_test_{Guid.NewGuid()}.json");
        try
        {
            var store = new PolicyStore(tempFile);
            var policy = store.Load();
            policy.IncludeDrivers = true;
            policy.HiddenKBs.Add("KB5031234");
            store.Save(policy);

            var reloaded = store.Load();
            Assert.True(reloaded.IncludeDrivers);
            Assert.Contains("KB5031234", reloaded.HiddenKBs);
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }
}
