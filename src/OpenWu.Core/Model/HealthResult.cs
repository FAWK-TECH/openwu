namespace OpenWu.Core.Model;

public sealed class HealthResult
{
    public bool IsElevated { get; init; }
    public bool IsDomainController { get; init; }
    public bool WuaServiceRunning { get; init; }
    public string WuaVersion { get; init; } = string.Empty;
    public bool CanSearch { get; init; }
    public string StatusMessage { get; init; } = string.Empty;
}
