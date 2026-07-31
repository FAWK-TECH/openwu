using System.Collections.Generic;
using OpenWu.Core.Guard;
using OpenWu.Core.Model;
using OpenWu.Core.Policy;
using Xunit;

namespace OpenWu.Core.Tests;

public sealed class SafetyGuardsTests
{
    [Theory]
    [InlineData("5031234", "KB5031234")]
    [InlineData("kb5031234", "KB5031234")]
    [InlineData("KB5031234", "KB5031234")]
    [InlineData("  5031234  ", "KB5031234")]
    public void NormalizeKb_FormatsCorrectly(string input, string expected)
    {
        var result = SafetyGuards.NormalizeKb(input);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void IsSecurityUpdate_IdentifiesSecurityAndCriticalUpdates()
    {
        var secRow = new UpdateRow
        {
            Kb = "KB5031234",
            Title = "2026-07 Cumulative Update for Windows 11",
            Categories = "Security Updates",
            Severity = "Important",
            Identity = "id1"
        };

        var driverRow = new UpdateRow
        {
            Kb = "N/A",
            Title = "Intel Graphics Driver",
            Categories = "Drivers",
            Severity = "Moderate",
            IsDriver = true,
            Identity = "id2"
        };

        Assert.True(SafetyGuards.IsSecurityUpdate(secRow));
        Assert.False(SafetyGuards.IsSecurityUpdate(driverRow));
    }

    [Fact]
    public void IsTitleDenied_BlocksDeniedKeywords()
    {
        var denyList = new[] { "Preview", "Beta" };
        Assert.True(SafetyGuards.IsTitleDenied("2026-07 Preview Cumulative Update", denyList));
        Assert.False(SafetyGuards.IsTitleDenied("2026-07 Cumulative Update", denyList));
    }

    [Fact]
    public void ValidateInstallRequest_BlocksDcInstallation_WhenNotAllowed()
    {
        var updates = new[]
        {
            new UpdateRow { Kb = "KB5031234", Title = "Update 1", Categories = "Security Updates", Severity = "Critical", Identity = "id1" }
        };

        var opts = new InstallOptions { AllowDomainController = false };
        var policy = new PolicyModel { AllowOnDomainController = false };

        var (allowed, reason) = SafetyGuards.ValidateInstallRequest(updates, opts, policy, isDomainControllerOverride: true);

        Assert.False(allowed);
        Assert.Contains("Domain Controller", reason);
    }
}
