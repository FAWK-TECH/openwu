namespace OpenWu.Core.Model;

public sealed class UpdateRow
{
    public required string Kb { get; init; }
    public required string Title { get; init; }
    public double SizeMB { get; init; }
    public required string Categories { get; init; }
    public required string Severity { get; init; }
    public bool IsDownloaded { get; init; }
    public bool IsHidden { get; init; }
    public bool IsDriver { get; init; }
    public bool RebootRequired { get; init; }
    public required string Identity { get; init; }
    public int Revision { get; init; }
    public string SupportUrl { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
}
