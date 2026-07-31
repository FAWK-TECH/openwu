namespace OpenWu.Core.Model;

public sealed class SearchOptions
{
    public bool IncludeDrivers { get; init; } = false;
    public bool IncludeHidden { get; init; } = false;
    public bool IncludeOptional { get; init; } = false;
    public bool UseMicrosoftUpdate { get; init; } = true;
}
