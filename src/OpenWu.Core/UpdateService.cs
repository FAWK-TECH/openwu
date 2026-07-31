using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using OpenWu.Core.Guard;
using OpenWu.Core.Model;
using OpenWu.Core.Policy;
using OpenWu.Core.Wua;

namespace OpenWu.Core;

public sealed class UpdateService
{
    private readonly WuaService _wua;
    private readonly PolicyStore _policyStore;

    public PolicyStore PolicyStore => _policyStore;

    public UpdateService(string? customPolicyPath = null)
    {
        _wua = new WuaService();
        _policyStore = new PolicyStore(customPolicyPath);
    }

    public Task<HealthResult> TestAsync(CancellationToken ct = default)
    {
        return _wua.TestAsync(ct);
    }

    public async Task<IReadOnlyList<UpdateRow>> SearchPendingAsync(
        SearchOptions? opt = null,
        IProgress<string>? status = null,
        CancellationToken ct = default)
    {
        var policy = _policyStore.Load();
        opt ??= new SearchOptions
        {
            IncludeDrivers = policy.IncludeDrivers,
            IncludeOptional = policy.IncludeOptional,
            UseMicrosoftUpdate = policy.Service.Equals("MicrosoftUpdate", StringComparison.OrdinalIgnoreCase)
        };

        var results = await _wua.SearchPendingAsync(opt, status, ct).ConfigureAwait(false);

        // Filter out policy hidden KBs if not explicitly searching hidden
        if (!opt.IncludeHidden && policy.HiddenKBs.Count > 0)
        {
            var hiddenSet = new HashSet<string>(policy.HiddenKBs.Select(SafetyGuards.NormalizeKb), StringComparer.OrdinalIgnoreCase);
            results = results.Where(r => !hiddenSet.Contains(r.Kb)).ToList();
        }

        return results;
    }

    public Task<IReadOnlyList<HistoryRow>> GetHistoryAsync(int last = 50, CancellationToken ct = default)
    {
        return _wua.GetHistoryAsync(last, ct);
    }

    public Task DownloadAsync(
        IEnumerable<UpdateRow> items,
        IProgress<OpProgress>? progress = null,
        CancellationToken ct = default)
    {
        return _wua.DownloadAsync(items, progress, ct);
    }

    public async Task<InstallResult> InstallAsync(
        IEnumerable<UpdateRow> items,
        InstallOptions opt,
        IProgress<OpProgress>? progress = null,
        CancellationToken ct = default)
    {
        var policy = _policyStore.Load();
        var validation = SafetyGuards.ValidateInstallRequest(items, opt, policy);
        if (!validation.Allowed)
        {
            return new InstallResult
            {
                Success = false,
                Message = validation.Reason,
                InstalledCount = 0,
                FailedCount = items.Count()
            };
        }

        var result = await _wua.InstallAsync(items, opt, progress, ct).ConfigureAwait(false);

        if (result.Success && opt.RebootIfRequired && result.RebootRequired)
        {
            // Execute reboot if opt.RebootIfRequired is checked
            try
            {
                System.Diagnostics.Process.Start("shutdown.exe", "/r /t 120 /c \"OpenWU: Automatic restart scheduled in 2 minutes following update installation.\"");
            }
            catch { }
        }

        return result;
    }

    public async Task HideAsync(
        IEnumerable<UpdateRow> items,
        bool persistPolicy = false,
        CancellationToken ct = default)
    {
        var list = items.ToList();
        await _wua.HideAsync(list, persistPolicy, ct).ConfigureAwait(false);

        if (persistPolicy)
        {
            var policy = _policyStore.Load();
            foreach (var item in list)
            {
                var norm = SafetyGuards.NormalizeKb(item.Kb);
                if (!string.IsNullOrWhiteSpace(norm) && norm != "N/A" && !policy.HiddenKBs.Contains(norm, StringComparer.OrdinalIgnoreCase))
                {
                    policy.HiddenKBs.Add(norm);
                }
            }
            _policyStore.Save(policy);
        }
    }

    public async Task UnhideAsync(IEnumerable<UpdateRow> items, CancellationToken ct = default)
    {
        var list = items.ToList();
        await _wua.UnhideAsync(list, ct).ConfigureAwait(false);

        var policy = _policyStore.Load();
        bool changed = false;
        foreach (var item in list)
        {
            var norm = SafetyGuards.NormalizeKb(item.Kb);
            if (policy.HiddenKBs.RemoveAll(x => x.Equals(norm, StringComparison.OrdinalIgnoreCase)) > 0)
            {
                changed = true;
            }
        }
        if (changed)
        {
            _policyStore.Save(policy);
        }
    }
}
