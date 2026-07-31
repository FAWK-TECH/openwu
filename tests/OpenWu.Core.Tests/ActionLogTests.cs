using System;
using System.IO;
using OpenWu.Core.Logging;
using Xunit;

namespace OpenWu.Core.Tests;

public sealed class ActionLogTests
{
    [Fact]
    public void Write_AppendsLineToLogFile()
    {
        var logDir = ActionLog.GetLogDirectory();
        var todayFile = Path.Combine(logDir, $"actions-{DateTime.UtcNow:yyyyMMdd}.log");

        ActionLog.Write("test_action", true, new[] { "KB123456" }, "Test action log entry");

        Assert.True(File.Exists(todayFile));
        var content = File.ReadAllText(todayFile);
        Assert.Contains("action=test_action", content);
        Assert.Contains("kbs=KB123456", content);
    }
}
