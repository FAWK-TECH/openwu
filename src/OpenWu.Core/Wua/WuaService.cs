using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using OpenWu.Core.Model;

namespace OpenWu.Core.Wua;

public sealed class WuaService
{
    private static readonly Regex KbRegex = new(@"KB\d+", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public Task<HealthResult> TestAsync(CancellationToken ct)
    {
        return Task.Run(() =>
        {
            try
            {
                if (!OperatingSystem.IsWindows())
                {
                    return new HealthResult
                    {
                        IsElevated = false,
                        IsDomainController = false,
                        WuaServiceRunning = false,
                        WuaVersion = "N/A (Non-Windows)",
                        CanSearch = false,
                        StatusMessage = "OpenWU Core requires Windows OS."
                    };
                }

                var sessionType = Type.GetTypeFromProgID("Microsoft.Update.Session");
                if (sessionType == null)
                {
                    return new HealthResult
                    {
                        IsElevated = Guard.SafetyGuards.IsElevated(),
                        IsDomainController = Guard.SafetyGuards.IsDomainController(),
                        WuaServiceRunning = false,
                        WuaVersion = "Not found",
                        CanSearch = false,
                        StatusMessage = "WUA COM object (Microsoft.Update.Session) is missing."
                    };
                }

                dynamic session = Activator.CreateInstance(sessionType)!;
                dynamic searcher = session.CreateUpdateSearcher();

                return new HealthResult
                {
                    IsElevated = Guard.SafetyGuards.IsElevated(),
                    IsDomainController = Guard.SafetyGuards.IsDomainController(),
                    WuaServiceRunning = true,
                    WuaVersion = "1.0 (COM)",
                    CanSearch = searcher != null,
                    StatusMessage = "WUA COM Session initialized successfully."
                };
            }
            catch (Exception ex)
            {
                return new HealthResult
                {
                    IsElevated = Guard.SafetyGuards.IsElevated(),
                    IsDomainController = Guard.SafetyGuards.IsDomainController(),
                    WuaServiceRunning = false,
                    WuaVersion = "Error",
                    CanSearch = false,
                    StatusMessage = $"WUA test error: {ex.Message}"
                };
            }
        }, ct);
    }

    public Task<IReadOnlyList<UpdateRow>> SearchPendingAsync(
        SearchOptions options,
        IProgress<string>? statusProgress,
        CancellationToken ct)
    {
        return Task.Run(() =>
        {
            var results = new List<UpdateRow>();
            statusProgress?.Report("Initializing WUA Searcher...");

            var sessionType = Type.GetTypeFromProgID("Microsoft.Update.Session")
                ?? throw new InvalidOperationException("WUA COM object Microsoft.Update.Session unavailable.");

            dynamic session = Activator.CreateInstance(sessionType)!;
            dynamic searcher = session.CreateUpdateSearcher();

            if (options.UseMicrosoftUpdate)
            {
                try
                {
                    statusProgress?.Report("Configuring Microsoft Update catalog...");
                    var mgrType = Type.GetTypeFromProgID("Microsoft.Update.ServiceManager");
                    if (mgrType != null)
                    {
                        dynamic mgr = Activator.CreateInstance(mgrType)!;
                        // ServiceID 7971f918-a847-4430-9279-4a52d1efe18d = Microsoft Update
                        mgr.AddService2("7971f918-a847-4430-9279-4a52d1efe18d", 7, "");
                        searcher.ServerSelection = 3; // ssOthers
                        searcher.ServiceID = "7971f918-a847-4430-9279-4a52d1efe18d";
                    }
                }
                catch
                {
                    // Ignore service manager registration errors, fallback to default WSUS/WU service
                }
            }

            string criteria = options.IncludeHidden ? "IsInstalled=0" : "IsInstalled=0 and IsHidden=0";
            statusProgress?.Report($"Executing search query: {criteria}");

            dynamic searchResult = searcher.Search(criteria);
            dynamic updates = searchResult.Updates;
            int count = updates.Count;

            statusProgress?.Report($"Processing {count} updates...");

            for (int i = 0; i < count; i++)
            {
                ct.ThrowIfCancellationRequested();
                dynamic u = updates.Item(i);

                bool isDriver = false;
                try
                {
                    int typeVal = u.Type;
                    isDriver = typeVal == 2; // 2 = Driver
                }
                catch { }

                if (!options.IncludeDrivers && isDriver)
                {
                    continue;
                }

                string title = u.Title ?? "Unknown Update";
                string kb = ExtractKbNumber(title, u);
                double sizeMB = 0;
                try
                {
                    double bytes = (double)u.MaxDownloadSize;
                    sizeMB = Math.Round(bytes / (1024 * 1024), 2);
                }
                catch { }

                string categories = GetCategories(u);
                string severity = GetSeverity(u);
                bool isDownloaded = false;
                bool isHidden = false;
                bool rebootReq = false;
                string identityStr = string.Empty;
                int revision = 1;
                string supportUrl = string.Empty;
                string description = string.Empty;

                try { isDownloaded = u.IsDownloaded; } catch { }
                try { isHidden = u.IsHidden; } catch { }
                try { rebootReq = u.RebootRequired; } catch { }
                try
                {
                    dynamic id = u.Identity;
                    identityStr = id.UpdateID ?? string.Empty;
                    revision = id.RevisionNumber;
                }
                catch { }
                try { supportUrl = u.SupportUrl ?? string.Empty; } catch { }
                try { description = u.Description ?? string.Empty; } catch { }

                var row = new UpdateRow
                {
                    Kb = kb,
                    Title = title,
                    SizeMB = sizeMB,
                    Categories = categories,
                    Severity = severity,
                    IsDownloaded = isDownloaded,
                    IsHidden = isHidden,
                    IsDriver = isDriver,
                    RebootRequired = rebootReq,
                    Identity = identityStr,
                    Revision = revision,
                    SupportUrl = supportUrl,
                    Description = description
                };

                results.Add(row);
            }

            statusProgress?.Report($"Search complete. Found {results.Count} matching updates.");
            return (IReadOnlyList<UpdateRow>)results;
        }, ct);
    }

    public Task<IReadOnlyList<HistoryRow>> GetHistoryAsync(int count, CancellationToken ct)
    {
        return Task.Run(() =>
        {
            var results = new List<HistoryRow>();
            if (!OperatingSystem.IsWindows()) return (IReadOnlyList<HistoryRow>)results;

            var sessionType = Type.GetTypeFromProgID("Microsoft.Update.Session");
            if (sessionType == null) return (IReadOnlyList<HistoryRow>)results;

            dynamic session = Activator.CreateInstance(sessionType)!;
            dynamic searcher = session.CreateUpdateSearcher();

            int totalHistory = searcher.GetTotalHistoryCount();
            int fetchCount = Math.Min(count, totalHistory);
            if (fetchCount <= 0) return (IReadOnlyList<HistoryRow>)results;

            dynamic historyColl = searcher.QueryHistory(0, fetchCount);
            int entriesCount = historyColl.Count;

            for (int i = 0; i < entriesCount; i++)
            {
                ct.ThrowIfCancellationRequested();
                dynamic entry = historyColl.Item(i);

                DateTime date = DateTime.MinValue;
                try { date = entry.Date; } catch { }

                string title = entry.Title ?? "Unknown Update";
                string kb = ExtractKbNumber(title, null);

                string resultStr = "Unknown";
                try
                {
                    int resultCode = entry.ResultCode;
                    resultStr = resultCode switch
                    {
                        1 => "In Progress",
                        2 => "Succeeded",
                        3 => "Succeeded with Errors",
                        4 => "Failed",
                        5 => "Aborted",
                        _ => $"Code {resultCode}"
                    };
                }
                catch { }

                string updateId = string.Empty;
                try
                {
                    dynamic id = entry.UpdateIdentity;
                    updateId = id.UpdateID ?? string.Empty;
                }
                catch { }

                results.Add(new HistoryRow
                {
                    Date = date,
                    Kb = kb,
                    Title = title,
                    Result = resultStr,
                    UpdateId = updateId
                });
            }

            return (IReadOnlyList<HistoryRow>)results;
        }, ct);
    }

    public Task DownloadAsync(
        IEnumerable<UpdateRow> items,
        IProgress<OpProgress>? progress,
        CancellationToken ct)
    {
        return Task.Run(() =>
        {
            var list = items.ToList();
            if (list.Count == 0) return;

            var sessionType = Type.GetTypeFromProgID("Microsoft.Update.Session")
                ?? throw new InvalidOperationException("WUA COM object unavailable.");

            dynamic session = Activator.CreateInstance(sessionType)!;
            dynamic searcher = session.CreateUpdateSearcher();
            dynamic searchResult = searcher.Search("IsInstalled=0");
            dynamic updates = searchResult.Updates;
            int totalComUpdates = updates.Count;

            var collType = Type.GetTypeFromProgID("Microsoft.Update.UpdateColl")
                ?? throw new InvalidOperationException("WUA UpdateColl object unavailable.");

            dynamic updateColl = Activator.CreateInstance(collType)!;

            var identitySet = new HashSet<string>(list.Select(x => x.Identity), StringComparer.OrdinalIgnoreCase);

            for (int i = 0; i < totalComUpdates; i++)
            {
                ct.ThrowIfCancellationRequested();
                dynamic u = updates.Item(i);
                string updateId = u.Identity.UpdateID;

                if (identitySet.Contains(updateId) && !u.IsDownloaded)
                {
                    updateColl.Add(u);
                }
            }

            if (updateColl.Count == 0)
            {
                progress?.Report(new OpProgress { Operation = "Download", Percent = 100, TotalCount = list.Count, CurrentIndex = list.Count });
                return;
            }

            dynamic downloader = session.CreateUpdateDownloader();
            downloader.Updates = updateColl;

            progress?.Report(new OpProgress
            {
                Operation = "Downloading updates...",
                Percent = 10,
                TotalCount = updateColl.Count,
                CurrentIndex = 0
            });

            downloader.Download();

            progress?.Report(new OpProgress
            {
                Operation = "Download complete.",
                Percent = 100,
                TotalCount = updateColl.Count,
                CurrentIndex = updateColl.Count
            });
        }, ct);
    }

    public Task<InstallResult> InstallAsync(
        IEnumerable<UpdateRow> items,
        InstallOptions opt,
        IProgress<OpProgress>? progress,
        CancellationToken ct)
    {
        return Task.Run(() =>
        {
            var list = items.ToList();
            if (list.Count == 0)
            {
                return new InstallResult
                {
                    Success = true,
                    Message = "No updates to install.",
                    InstalledCount = 0,
                    FailedCount = 0
                };
            }

            if (opt.WhatIf)
            {
                return new InstallResult
                {
                    Success = true,
                    Message = $"[WhatIf] Would install {list.Count} update(s): {string.Join(", ", list.Select(x => x.Kb))}",
                    InstalledCount = list.Count,
                    FailedCount = 0,
                    InstalledKbs = list.Select(x => x.Kb).ToList()
                };
            }

            var sessionType = Type.GetTypeFromProgID("Microsoft.Update.Session")
                ?? throw new InvalidOperationException("WUA COM object unavailable.");

            dynamic session = Activator.CreateInstance(sessionType)!;
            dynamic searcher = session.CreateUpdateSearcher();
            dynamic searchResult = searcher.Search("IsInstalled=0");
            dynamic updates = searchResult.Updates;
            int totalComUpdates = updates.Count;

            var collType = Type.GetTypeFromProgID("Microsoft.Update.UpdateColl")
                ?? throw new InvalidOperationException("WUA UpdateColl object unavailable.");

            dynamic updateColl = Activator.CreateInstance(collType)!;
            var identitySet = new HashSet<string>(list.Select(x => x.Identity), StringComparer.OrdinalIgnoreCase);

            for (int i = 0; i < totalComUpdates; i++)
            {
                ct.ThrowIfCancellationRequested();
                dynamic u = updates.Item(i);
                string updateId = u.Identity.UpdateID;

                if (identitySet.Contains(updateId))
                {
                    // Ensure EULA accepted
                    if (!u.EulaAccepted)
                    {
                        u.AcceptEula();
                    }
                    updateColl.Add(u);
                }
            }

            if (updateColl.Count == 0)
            {
                return new InstallResult
                {
                    Success = false,
                    Message = "None of the selected updates were found in pending state.",
                    InstalledCount = 0,
                    FailedCount = list.Count
                };
            }

            // Download first if needed
            dynamic downloader = session.CreateUpdateDownloader();
            downloader.Updates = updateColl;
            progress?.Report(new OpProgress { Operation = "Downloading before install...", Percent = 25, TotalCount = updateColl.Count });
            downloader.Download();

            dynamic installer = session.CreateUpdateInstaller();
            installer.Updates = updateColl;

            progress?.Report(new OpProgress { Operation = "Installing updates...", Percent = 50, TotalCount = updateColl.Count });

            dynamic result = installer.Install();
            int resultCode = result.ResultCode; // 2 = Succeeded, 3 = Succeeded with errors
            bool rebootReq = result.RebootRequired;

            bool success = resultCode == 2 || resultCode == 3;
            var installedKbs = list.Select(x => x.Kb).ToList();

            return new InstallResult
            {
                Success = success,
                RebootRequired = rebootReq,
                InstalledCount = success ? list.Count : 0,
                FailedCount = success ? 0 : list.Count,
                InstalledKbs = installedKbs,
                Message = success ? $"Install finished (ResultCode: {resultCode}, RebootRequired: {rebootReq})" : $"Install failed with ResultCode: {resultCode}"
            };
        }, ct);
    }

    public Task HideAsync(IEnumerable<UpdateRow> items, bool persistPolicy, CancellationToken ct)
    {
        return Task.Run(() =>
        {
            SetHiddenState(items, true, ct);
        }, ct);
    }

    public Task UnhideAsync(IEnumerable<UpdateRow> items, CancellationToken ct)
    {
        return Task.Run(() =>
        {
            SetHiddenState(items, false, ct);
        }, ct);
    }

    private static void SetHiddenState(IEnumerable<UpdateRow> items, bool hideState, CancellationToken ct)
    {
        var list = items.ToList();
        if (list.Count == 0) return;

        var sessionType = Type.GetTypeFromProgID("Microsoft.Update.Session")
            ?? throw new InvalidOperationException("WUA COM object unavailable.");

        dynamic session = Activator.CreateInstance(sessionType)!;
        dynamic searcher = session.CreateUpdateSearcher();
        dynamic searchResult = searcher.Search("IsInstalled=0");
        dynamic updates = searchResult.Updates;
        int count = updates.Count;

        var identitySet = new HashSet<string>(list.Select(x => x.Identity), StringComparer.OrdinalIgnoreCase);

        for (int i = 0; i < count; i++)
        {
            ct.ThrowIfCancellationRequested();
            dynamic u = updates.Item(i);
            string updateId = u.Identity.UpdateID;

            if (identitySet.Contains(updateId))
            {
                u.IsHidden = hideState;
            }
        }
    }

    private static string ExtractKbNumber(string title, dynamic? updateObj)
    {
        if (updateObj != null)
        {
            try
            {
                dynamic? kbs = updateObj.KBArticleIDs;
                if (kbs is not null)
                {
                    int kbCount = 0;
                    try { kbCount = (int)kbs.Count; } catch { kbCount = 0; }
                    if (kbCount > 0)
                    {
                        object? first = kbs.Item(0);
                        if (first is not null)
                            return "KB" + first.ToString();
                    }
                }
            }
            catch { }
        }

        var match = KbRegex.Match(title);
        if (match.Success)
        {
            return match.Value.ToUpperInvariant();
        }

        return "N/A";
    }

    private static string GetCategories(dynamic updateObj)
    {
        try
        {
            dynamic? cats = updateObj.Categories;
            if (cats is null) return "General";
            int count = 0;
            try { count = (int)cats.Count; } catch { return "General"; }
            if (count == 0) return "General";
            var list = new List<string>();
            for (int i = 0; i < count; i++)
            {
                string? name = null;
                try { name = cats.Item(i)?.Name as string; } catch { }
                if (!string.IsNullOrWhiteSpace(name)) list.Add(name!);
            }
            return list.Count > 0 ? string.Join(", ", list) : "General";
        }
        catch
        {
            return "General";
        }
    }

    private static string GetSeverity(dynamic updateObj)
    {
        try
        {
            int msi = updateObj.MsiApplicabilityResult;
        }
        catch { }

        try
        {
            string title = updateObj.Title ?? "";
            if (title.Contains("Critical", StringComparison.OrdinalIgnoreCase)) return "Critical";
            if (title.Contains("Security", StringComparison.OrdinalIgnoreCase)) return "Important";
        }
        catch { }

        return "Moderate";
    }
}
