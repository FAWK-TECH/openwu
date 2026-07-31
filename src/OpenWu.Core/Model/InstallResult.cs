using System.Collections.Generic;

namespace OpenWu.Core.Model;

public sealed class InstallResult
{
    public bool Success { get; init; }
    public bool RebootRequired { get; init; }
    public int InstalledCount { get; init; }
    public int FailedCount { get; init; }
    public IReadOnlyList<string> InstalledKbs { get; init; } = new List<string>();
    public IReadOnlyList<string> FailedKbs { get; init; } = new List<string>();
    public string Message { get; init; } = string.Empty;
}
