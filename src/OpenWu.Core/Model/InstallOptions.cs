namespace OpenWu.Core.Model;

public sealed class InstallOptions
{
    public bool WhatIf { get; init; } = false;
    public bool Force { get; init; } = false;
    public bool RebootIfRequired { get; init; } = false;
    public bool AllowDomainController { get; init; } = false;
    public bool SecurityOnly { get; init; } = false;
}
