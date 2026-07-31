using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using OpenWu.Core.Guard;

namespace OpenWu.Core.Logging;

public static class ActionLog
{
    private static readonly object LogLock = new();

    public static string GetLogDirectory()
    {
        var programData = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
        return Path.Combine(programData, "OpenWU", "logs");
    }

    public static void Write(
        string action,
        bool ok,
        IEnumerable<string>? kbs = null,
        string? message = null,
        IReadOnlyDictionary<string, string>? extra = null)
    {
        try
        {
            var dir = GetLogDirectory();
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            var logFile = Path.Combine(dir, $"actions-{DateTime.UtcNow:yyyyMMdd}.log");

            var timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");
            var host = Environment.MachineName;
            var user = $"{Environment.UserDomainName}\\{Environment.UserName}";
            var elevated = SafetyGuards.IsElevated().ToString().ToLowerInvariant();

            var kbStr = (kbs != null && kbs.Any()) ? string.Join(",", kbs.Select(SafetyGuards.NormalizeKb)) : "none";
            var msgStr = !string.IsNullOrWhiteSpace(message) ? message.Replace('\n', ' ').Replace('\r', ' ') : "N/A";

            var line = $"{timestamp} host={host} user={user} elevated={elevated} action={action} ok={ok.ToString().ToLowerInvariant()} kbs={kbStr} message=\"{msgStr}\"";

            if (extra != null && extra.Count > 0)
            {
                foreach (var kvp in extra)
                {
                    line += $" {kvp.Key}={kvp.Value}";
                }
            }

            lock (LogLock)
            {
                File.AppendAllText(logFile, line + Environment.NewLine);
            }
        }
        catch
        {
            // Logging should never throw unhandled exceptions to callers
        }
    }
}
