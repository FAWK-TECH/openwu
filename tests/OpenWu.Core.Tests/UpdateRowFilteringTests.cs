using System.Collections.Generic;
using System.Linq;
using OpenWu.Core.Guard;
using OpenWu.Core.Model;
using Xunit;

namespace OpenWu.Core.Tests;

public sealed class UpdateRowFilteringTests
{
    [Fact]
    public void FilterSecurityOnly_ExcludesDriversAndNonSecurity()
    {
        var rows = new List<UpdateRow>
        {
            new UpdateRow { Kb = "KB1", Title = "Security Patch", Categories = "Security Updates", Severity = "Critical", IsDriver = false, Identity = "1" },
            new UpdateRow { Kb = "KB2", Title = "Driver Patch", Categories = "Drivers", Severity = "Moderate", IsDriver = true, Identity = "2" },
            new UpdateRow { Kb = "KB3", Title = "Feature Pack", Categories = "Updates", Severity = "Moderate", IsDriver = false, Identity = "3" }
        };

        var securityOnly = rows.Where(SafetyGuards.IsSecurityUpdate).ToList();

        Assert.Single(securityOnly);
        Assert.Equal("KB1", securityOnly[0].Kb);
    }
}
