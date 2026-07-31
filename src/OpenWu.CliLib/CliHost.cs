using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OpenWu.Core;
using OpenWu.Core.Guard;
using OpenWu.Core.Model;
using OpenWu.Core.Policy;

namespace OpenWu.CliLib;

public static class CliHost
{
    public static int Run(string[] args)
    {
        try
        {
            return RunAsync(args).GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            if (args.Any(a => a.Equals("--json", StringComparison.OrdinalIgnoreCase)))
                JsonEnvelope.Write(JsonEnvelope.Error("unknown", ex.Message, 3));
            else
                Console.Error.WriteLine($"[ERROR] Unhandled CLI exception: {ex.Message}");
            return 3;
        }
    }

    private static async Task<int> RunAsync(string[] args)
    {
        if (args.Length == 0 || HasFlag(args, "--help") || HasFlag(args, "-h") ||
            args[0].Equals("help", StringComparison.OrdinalIgnoreCase))
        {
            ShowHelp();
            return 0;
        }

        if (HasFlag(args, "--version") || HasFlag(args, "-v") ||
            args[0].Equals("version", StringComparison.OrdinalIgnoreCase))
        {
            if (HasFlag(args, "--json"))
                JsonEnvelope.Write(JsonEnvelope.Version());
            else
                Console.WriteLine($"OpenWU v{JsonEnvelope.AppVersion}");
            return 0;
        }

        string verb = args[0].ToLowerInvariant();
        bool json = HasFlag(args, "--json");
        var service = new UpdateService();

        return verb switch
        {
            "test" => await HandleTestAsync(service, json),
            "list" => await HandleListAsync(service, args, json),
            "history" => await HandleHistoryAsync(service, args, json),
            "install" => await HandleInstallAsync(service, args, json),
            "hide" => await HandleHideAsync(service, args, json),
            "show" => await HandleShowAsync(service, args, json),
            "policy" => HandlePolicy(service, args, json),
            _ => UnknownVerb(args[0], json)
        };
    }

    private static int UnknownVerb(string verb, bool json)
    {
        var msg = $"Unknown command verb '{verb}'.";
        if (json)
            JsonEnvelope.Write(JsonEnvelope.Error(verb, msg, 1));
        else
        {
            Console.Error.WriteLine($"[ERROR] {msg}");
            ShowHelp();
        }
        return 1;
    }

    private static async Task<int> HandleTestAsync(UpdateService service, bool json)
    {
        var res = await service.TestAsync();
        bool ok = res.CanSearch;

        if (json)
        {
            JsonEnvelope.Write(JsonEnvelope.Test(ok, new
            {
                isElevated = res.IsElevated,
                isDomainController = res.IsDomainController,
                wuaServiceRunning = res.WuaServiceRunning,
                wuaVersion = res.WuaVersion,
                canSearch = res.CanSearch,
                statusMessage = res.StatusMessage
            }, res.StatusMessage));
        }
        else
        {
            Console.WriteLine($"Elevation:          {res.IsElevated}");
            Console.WriteLine($"Domain Controller:  {res.IsDomainController}");
            Console.WriteLine($"WUA Service:        {res.WuaServiceRunning}");
            Console.WriteLine($"WUA Version:        {res.WuaVersion}");
            Console.WriteLine($"Can Search:         {res.CanSearch}");
            Console.WriteLine($"Status:             {res.StatusMessage}");
            Console.WriteLine($"OpenWU Version:     {JsonEnvelope.AppVersion}");
        }

        return ok ? 0 : 3;
    }

    private static async Task<int> HandleListAsync(UpdateService service, string[] args, bool json)
    {
        var opts = new SearchOptions
        {
            IncludeDrivers = HasFlag(args, "--include-drivers"),
            IncludeHidden = HasFlag(args, "--include-hidden")
        };

        var statusProgress = json ? null : new Progress<string>(msg => Console.WriteLine($"[SEARCH] {msg}"));
        var items = await service.SearchPendingAsync(opts, statusProgress);

        if (json)
        {
            var rows = items.Select(MapUpdate).Cast<object>().ToList();
            JsonEnvelope.Write(JsonEnvelope.List(ok: true, rows));
        }
        else
        {
            Console.WriteLine();
            Console.WriteLine($"Found {items.Count} update(s):");
            Console.WriteLine(new string('-', 85));
            Console.WriteLine($"{"KB",-12} | {"Size(MB)",-8} | {"Severity",-10} | {"Category",-18} | {"Title"}");
            Console.WriteLine(new string('-', 85));
            foreach (var u in items)
            {
                string titleCut = u.Title.Length > 32 ? u.Title[..29] + "..." : u.Title;
                string catCut = u.Categories.Length > 18 ? u.Categories[..15] + "..." : u.Categories;
                Console.WriteLine($"{u.Kb,-12} | {u.SizeMB,8:F1} | {u.Severity,-10} | {catCut,-18} | {titleCut}");
            }
        }

        return 0;
    }

    private static async Task<int> HandleHistoryAsync(UpdateService service, string[] args, bool json)
    {
        int last = GetIntArg(args, "--last", 20);
        var history = await service.GetHistoryAsync(last);

        if (json)
        {
            var rows = history.Select(h => (object)new
            {
                date = h.Date,
                kb = h.Kb,
                title = h.Title,
                result = h.Result,
                updateId = h.UpdateId
            }).ToList();
            JsonEnvelope.Write(JsonEnvelope.History(ok: true, rows, last));
        }
        else
        {
            Console.WriteLine($"Windows Update History (last {history.Count}):");
            Console.WriteLine(new string('-', 80));
            Console.WriteLine($"{"Date",-20} | {"KB",-12} | {"Result",-20} | {"Title"}");
            Console.WriteLine(new string('-', 80));
            foreach (var h in history)
            {
                string dateStr = h.Date.ToString("yyyy-MM-dd HH:mm");
                string titleCut = h.Title.Length > 30 ? h.Title[..27] + "..." : h.Title;
                Console.WriteLine($"{dateStr,-20} | {h.Kb,-12} | {h.Result,-20} | {titleCut}");
            }
        }

        return 0;
    }

    private static async Task<int> HandleInstallAsync(UpdateService service, string[] args, bool json)
    {
        if (!SafetyGuards.IsElevated())
            return Fail("install", "Installation requires administrative elevation.", 2, json);

        bool whatif = HasFlag(args, "--whatif");
        bool force = HasFlag(args, "--force");
        bool reboot = HasFlag(args, "--reboot");
        bool allowDc = HasFlag(args, "--allow-domain-controller");
        bool securityOnly = HasFlag(args, "--security-only");
        bool installAll = HasFlag(args, "--all");
        var kbArgs = GetValuesForFlag(args, "--kb");

        var statusProgress = json ? null : new Progress<string>(msg => Console.WriteLine($"[SEARCH] {msg}"));
        var items = await service.SearchPendingAsync(null, statusProgress);

        List<UpdateRow> targets;
        if (kbArgs.Count > 0)
        {
            var kbSet = new HashSet<string>(kbArgs.Select(SafetyGuards.NormalizeKb), StringComparer.OrdinalIgnoreCase);
            targets = items.Where(u => kbSet.Contains(SafetyGuards.NormalizeKb(u.Kb))).ToList();
        }
        else if (securityOnly)
            targets = items.Where(SafetyGuards.IsSecurityUpdate).ToList();
        else if (installAll)
            targets = items.ToList();
        else
            return Fail("install", "Must specify --all, --security-only, or --kb <KB> for install.", 1, json);

        var selectedKbs = targets.Select(t => t.Kb).Where(k => !string.IsNullOrWhiteSpace(k)).ToList();

        if (targets.Count == 0)
        {
            if (json)
            {
                JsonEnvelope.Write(JsonEnvelope.Install(
                    ok: true, whatIf: whatif,
                    selected: Array.Empty<string>(),
                    installed: Array.Empty<string>(),
                    failed: Array.Empty<string>(),
                    rebootRequired: false,
                    message: "No matching updates found to install."));
            }
            else
                Console.WriteLine("No matching updates found to install.");
            return 0;
        }

        var installOpts = new InstallOptions
        {
            WhatIf = whatif,
            Force = force,
            RebootIfRequired = reboot,
            AllowDomainController = allowDc,
            SecurityOnly = securityOnly
        };

        var progress = json ? null : new Progress<OpProgress>(p => Console.WriteLine($"[{p.Percent}%] {p.Operation}"));
        var result = await service.InstallAsync(targets, installOpts, progress);

        var installed = result.InstalledKbs?.ToList() ?? new List<string>();
        var failed = result.FailedKbs?.ToList() ?? new List<string>();

        if (json)
        {
            JsonEnvelope.Write(JsonEnvelope.Install(
                ok: result.Success,
                whatIf: whatif,
                selected: selectedKbs,
                installed: installed,
                failed: failed,
                rebootRequired: result.RebootRequired,
                message: result.Message));
        }
        else
        {
            Console.WriteLine();
            Console.WriteLine($"Install Result: {(result.Success ? "SUCCESS" : "FAILED")}");
            Console.WriteLine($"Message: {result.Message}");
            Console.WriteLine($"Reboot Required: {result.RebootRequired}");
        }

        if (!result.Success && result.Message.Contains("Domain Controller", StringComparison.OrdinalIgnoreCase))
            return 4;

        return result.Success ? 0 : 3;
    }

    private static async Task<int> HandleHideAsync(UpdateService service, string[] args, bool json)
    {
        if (!SafetyGuards.IsElevated())
            return Fail("hide", "Hiding updates requires administrative elevation.", 2, json);

        bool persist = HasFlag(args, "--persist");
        var kbs = GetValuesForFlag(args, "--kb").Select(SafetyGuards.NormalizeKb).ToList();
        if (kbs.Count == 0)
            return Fail("hide", "Must specify --kb <KB> to hide.", 1, json);

        var items = await service.SearchPendingAsync(new SearchOptions { IncludeHidden = true });
        var kbSet = new HashSet<string>(kbs, StringComparer.OrdinalIgnoreCase);
        var targets = items.Where(u => kbSet.Contains(SafetyGuards.NormalizeKb(u.Kb))).ToList();
        var matched = targets.Select(t => SafetyGuards.NormalizeKb(t.Kb)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();

        if (targets.Count == 0)
        {
            if (json)
                JsonEnvelope.Write(JsonEnvelope.HideShow("hide", true, kbs, persist, "No matching updates found for specified KB(s)."));
            else
                Console.WriteLine("No matching updates found for specified KB(s).");
            return 0;
        }

        await service.HideAsync(targets, persist);

        if (json)
            JsonEnvelope.Write(JsonEnvelope.HideShow("hide", true, matched, persist, $"Successfully hid {targets.Count} update(s)."));
        else
            Console.WriteLine($"Successfully hid {targets.Count} update(s). (Persist policy: {persist})");
        return 0;
    }

    private static async Task<int> HandleShowAsync(UpdateService service, string[] args, bool json)
    {
        if (!SafetyGuards.IsElevated())
            return Fail("show", "Unhiding updates requires administrative elevation.", 2, json);

        var kbs = GetValuesForFlag(args, "--kb").Select(SafetyGuards.NormalizeKb).ToList();
        if (kbs.Count == 0)
            return Fail("show", "Must specify --kb <KB> to unhide.", 1, json);

        var items = await service.SearchPendingAsync(new SearchOptions { IncludeHidden = true });
        var kbSet = new HashSet<string>(kbs, StringComparer.OrdinalIgnoreCase);
        var targets = items.Where(u => kbSet.Contains(SafetyGuards.NormalizeKb(u.Kb))).ToList();
        var matched = targets.Select(t => SafetyGuards.NormalizeKb(t.Kb)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();

        if (targets.Count == 0)
        {
            if (json)
                JsonEnvelope.Write(JsonEnvelope.HideShow("show", true, kbs, false, "No matching updates found for specified KB(s)."));
            else
                Console.WriteLine("No matching updates found for specified KB(s).");
            return 0;
        }

        await service.UnhideAsync(targets);

        if (json)
            JsonEnvelope.Write(JsonEnvelope.HideShow("show", true, matched, false, $"Successfully unhid {targets.Count} update(s)."));
        else
            Console.WriteLine($"Successfully unhid {targets.Count} update(s).");
        return 0;
    }

    private static int HandlePolicy(UpdateService service, string[] args, bool json)
    {
        if (args.Length < 2)
            return Fail("policy", "Missing policy sub-command. Usage: policy show|set|reset", 1, json);

        string sub = args[1].ToLowerInvariant();
        var store = service.PolicyStore;

        if (sub == "show")
        {
            var p = store.Load();
            if (json)
                JsonEnvelope.Write(JsonEnvelope.Policy(true, "show", MapPolicy(p)));
            else
                JsonEnvelope.Write(MapPolicy(p));
            return 0;
        }

        if (sub == "reset")
        {
            store.Reset();
            if (json)
                JsonEnvelope.Write(JsonEnvelope.Policy(true, "reset", MapPolicy(store.Load()), "Policy reset to defaults."));
            else
                Console.WriteLine("Policy reset to default configuration.");
            return 0;
        }

        if (sub == "set")
        {
            var p = store.Load();
            for (int i = 2; i < args.Length - 1; i += 2)
            {
                string key = args[i].TrimStart('-');
                string val = args[i + 1];
                if (key.Equals("includeDrivers", StringComparison.OrdinalIgnoreCase) ||
                    key.Equals("include-drivers", StringComparison.OrdinalIgnoreCase))
                    p.IncludeDrivers = bool.Parse(val);
                else if (key.Equals("allowOnDomainController", StringComparison.OrdinalIgnoreCase) ||
                         key.Equals("allow-on-domain-controller", StringComparison.OrdinalIgnoreCase))
                    p.AllowOnDomainController = bool.Parse(val);
                else if (key.Equals("service", StringComparison.OrdinalIgnoreCase))
                    p.Service = val;
                else if (key.Equals("reboot", StringComparison.OrdinalIgnoreCase))
                    p.Reboot = val;
            }

            store.Save(p);
            if (json)
                JsonEnvelope.Write(JsonEnvelope.Policy(true, "set", MapPolicy(p), "Policy updated successfully."));
            else
                Console.WriteLine("Policy updated successfully.");
            return 0;
        }

        return Fail("policy", $"Unknown policy sub-command '{args[1]}'.", 1, json);
    }

    private static int Fail(string verb, string message, int exitCode, bool json)
    {
        if (json)
            JsonEnvelope.Write(JsonEnvelope.Error(verb, message, exitCode));
        else
            Console.Error.WriteLine($"[ERROR] {message}");
        return exitCode;
    }

    private static object MapUpdate(UpdateRow u) => new
    {
        kb = u.Kb,
        title = u.Title,
        sizeMB = u.SizeMB,
        categories = u.Categories,
        severity = u.Severity,
        isDownloaded = u.IsDownloaded,
        isHidden = u.IsHidden,
        isDriver = u.IsDriver,
        rebootRequired = u.RebootRequired,
        identity = u.Identity,
        revision = u.Revision,
        supportUrl = u.SupportUrl,
        description = u.Description
    };

    private static object MapPolicy(PolicyModel p) => new
    {
        schemaVersion = p.SchemaVersion,
        service = p.Service,
        includeDrivers = p.IncludeDrivers,
        includeOptional = p.IncludeOptional,
        autoSelect = p.AutoSelect,
        reboot = p.Reboot,
        hiddenKBs = p.HiddenKBs,
        denyTitlesContains = p.DenyTitlesContains,
        allowOnDomainController = p.AllowOnDomainController,
        maxInstallBatch = p.MaxInstallBatch,
        searchTimeoutSec = p.SearchTimeoutSec
    };

    private static bool HasFlag(string[] args, string flag) =>
        args.Any(a => a.Equals(flag, StringComparison.OrdinalIgnoreCase));

    private static int GetIntArg(string[] args, string flag, int defaultValue)
    {
        for (int i = 0; i < args.Length - 1; i++)
        {
            if (args[i].Equals(flag, StringComparison.OrdinalIgnoreCase) && int.TryParse(args[i + 1], out int val))
                return val;
        }
        return defaultValue;
    }

    private static List<string> GetValuesForFlag(string[] args, string flag)
    {
        var result = new List<string>();
        for (int i = 0; i < args.Length - 1; i++)
        {
            if (args[i].Equals(flag, StringComparison.OrdinalIgnoreCase))
                result.Add(args[i + 1]);
        }
        return result;
    }

    private static void ShowHelp()
    {
        Console.WriteLine($@"OpenWU v{JsonEnvelope.AppVersion} - Universal Windows Update Manager

Usage:
  OpenWU.exe                             (Starts the WinForms GUI)
  openwu-cli.exe test [--json]
  openwu-cli.exe list [--json] [--include-drivers] [--include-hidden]
  openwu-cli.exe history [--last N] [--json]
  openwu-cli.exe install --security-only|--all|--kb KB... [--whatif] [--force] [--reboot] [--allow-domain-controller] [--json]
  openwu-cli.exe hide --kb KB... [--persist] [--json]
  openwu-cli.exe show --kb KB... [--json]
  openwu-cli.exe policy show|set|reset [--json]
  openwu-cli.exe --help
  openwu-cli.exe --version [--json]

JSON output is always an object envelope (ok, host, version, verb, ...) - never a bare array.
");
    }
}
