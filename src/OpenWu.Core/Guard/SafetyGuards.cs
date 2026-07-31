using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Security.Principal;
using OpenWu.Core.Model;
using OpenWu.Core.Policy;

namespace OpenWu.Core.Guard;

public static class SafetyGuards
{
    public static bool IsElevated()
    {
        if (!OperatingSystem.IsWindows()) return false;
        using var identity = WindowsIdentity.GetCurrent();
        var principal = new WindowsPrincipal(identity);
        return principal.IsInRole(WindowsBuiltInRole.Administrator);
    }

    public static bool IsDomainController()
    {
        if (!OperatingSystem.IsWindows()) return false;
        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT ProductType FROM Win32_OperatingSystem");
            foreach (ManagementObject obj in searcher.Get())
            {
                if (obj["ProductType"] is uint productType)
                {
                    // ProductType 2 = Domain Controller
                    return productType == 2;
                }
            }
        }
        catch
        {
            // If WMI query fails, play safe or assume non-DC
        }
        return false;
    }

    public static string NormalizeKb(string kbInput)
    {
        if (string.IsNullOrWhiteSpace(kbInput)) return string.Empty;
        var trimmed = kbInput.Trim().ToUpperInvariant();
        if (!trimmed.StartsWith("KB") && trimmed.All(char.IsDigit))
        {
            return "KB" + trimmed;
        }
        return trimmed;
    }

    public static bool IsSecurityUpdate(UpdateRow update)
    {
        if (update.IsDriver) return false;

        var cat = update.Categories ?? string.Empty;
        var sev = update.Severity ?? string.Empty;

        if (cat.Contains("Security", StringComparison.OrdinalIgnoreCase) ||
            cat.Contains("Critical", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (sev.Equals("Critical", StringComparison.OrdinalIgnoreCase) ||
            sev.Equals("Important", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return false;
    }

    public static bool IsTitleDenied(string title, IEnumerable<string> denyKeywords)
    {
        if (string.IsNullOrWhiteSpace(title)) return false;
        foreach (var keyword in denyKeywords)
        {
            if (!string.IsNullOrWhiteSpace(keyword) && title.Contains(keyword, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }
        return false;
    }

    public static (bool Allowed, string Reason) ValidateInstallRequest(
        IEnumerable<UpdateRow> updates,
        InstallOptions options,
        PolicyModel policy,
        bool isDomainControllerOverride = false)
    {
        var list = updates.ToList();
        if (list.Count == 0)
        {
            return (false, "No updates selected for installation.");
        }

        bool isDc = isDomainControllerOverride || IsDomainController();
        if (isDc && !options.AllowDomainController && !policy.AllowOnDomainController)
        {
            return (false, "Installation on a Domain Controller is blocked. Enable 'Allow on Domain Controller' or pass --allow-domain-controller to proceed.");
        }

        if (!options.Force)
        {
            var deniedUpdates = list.Where(u => IsTitleDenied(u.Title, policy.DenyTitlesContains)).ToList();
            if (deniedUpdates.Count > 0)
            {
                var titles = string.Join(", ", deniedUpdates.Select(u => u.Title));
                return (false, $"The following update(s) match denied title filters ({string.Join(", ", policy.DenyTitlesContains)}): {titles}. Pass --force or uncheck title denial to override.");
            }
        }

        return (true, "Validation succeeded.");
    }
}
