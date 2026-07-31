using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace OpenWu.CliLib;

/// <summary>
/// Stable CLI JSON contract. Always wrap payloads — never emit a bare array.
/// </summary>
public static class JsonEnvelope
{
    public static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public static string AppVersion
    {
        get
        {
            var asm = typeof(JsonEnvelope).Assembly;
            // Prefer informational version from entry assembly (openwu-cli / OpenWU)
            var entry = Assembly.GetEntryAssembly() ?? asm;
            var info = entry.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
            if (!string.IsNullOrWhiteSpace(info))
            {
                var plus = info.IndexOf('+', StringComparison.Ordinal);
                return plus > 0 ? info[..plus] : info;
            }

            return entry.GetName().Version?.ToString(3) ?? "0.2.0";
        }
    }

    public static string HostName
    {
        get
        {
            try { return Environment.MachineName; }
            catch { return "unknown"; }
        }
    }

    public static void Write(object envelope) =>
        Console.WriteLine(JsonSerializer.Serialize(envelope, Options));

    public static object Test(bool ok, object health, string? message = null) => new
    {
        ok,
        host = HostName,
        version = AppVersion,
        verb = "test",
        message,
        health
    };

    public static object List(bool ok, IReadOnlyList<object> updates, string? message = null) => new
    {
        ok,
        host = HostName,
        version = AppVersion,
        verb = "list",
        message,
        count = updates.Count,
        updates
    };

    public static object History(bool ok, IReadOnlyList<object> entries, int requestedLast, string? message = null) => new
    {
        ok,
        host = HostName,
        version = AppVersion,
        verb = "history",
        message,
        requestedLast,
        count = entries.Count,
        history = entries
    };

    public static object Install(
        bool ok,
        bool whatIf,
        IReadOnlyList<string> selected,
        IReadOnlyList<string> installed,
        IReadOnlyList<string> failed,
        bool rebootRequired,
        string? message = null) => new
    {
        ok,
        host = HostName,
        version = AppVersion,
        verb = "install",
        message,
        whatIf,
        selected,
        installed,
        failed,
        rebootRequired,
        countSelected = selected.Count,
        countInstalled = installed.Count,
        countFailed = failed.Count
    };

    public static object HideShow(string verb, bool ok, IReadOnlyList<string> kbs, bool persist, string? message = null) => new
    {
        ok,
        host = HostName,
        version = AppVersion,
        verb,
        message,
        persist,
        count = kbs.Count,
        kbs
    };

    public static object Policy(bool ok, string action, object? policy, string? message = null) => new
    {
        ok,
        host = HostName,
        version = AppVersion,
        verb = "policy",
        action,
        message,
        policy
    };

    public static object Error(string verb, string message, int exitCode) => new
    {
        ok = false,
        host = HostName,
        version = AppVersion,
        verb,
        message,
        exitCode
    };

    public static object Version() => new
    {
        ok = true,
        host = HostName,
        version = AppVersion,
        verb = "version",
        product = "OpenWU"
    };
}
