using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace OpenWu.App.Cli;

/// <summary>
/// Stable CLI JSON contract for automation. Always wrap payloads — never emit a bare array.
/// </summary>
internal static class JsonEnvelope
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
            var info = asm.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
            if (!string.IsNullOrWhiteSpace(info))
            {
                // Strip any SourceLink "+commit" suffix for display
                var plus = info.IndexOf('+', StringComparison.Ordinal);
                return plus > 0 ? info[..plus] : info;
            }

            return asm.GetName().Version?.ToString(3) ?? "0.1.0";
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

    public static void WriteSuccess(string verb, object payloadFields)
    {
        // payloadFields is merged conceptually via anonymous object built by callers
        Console.WriteLine(JsonSerializer.Serialize(payloadFields, Options));
    }

    public static object Base(string verb, bool ok, string? message = null) => new
    {
        ok,
        host = HostName,
        version = AppVersion,
        verb,
        message
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

    public static object Test(bool ok, object health, string? message = null) => new
    {
        ok,
        host = HostName,
        version = AppVersion,
        verb = "test",
        message,
        health
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

    public static void Write(object envelope) =>
        Console.WriteLine(JsonSerializer.Serialize(envelope, Options));
}
