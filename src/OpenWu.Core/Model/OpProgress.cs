namespace OpenWu.Core.Model;

public sealed class OpProgress
{
    public string Operation { get; init; } = string.Empty;
    public string CurrentKb { get; init; } = string.Empty;
    public string CurrentTitle { get; init; } = string.Empty;
    public int Percent { get; init; }
    public int CurrentIndex { get; init; }
    public int TotalCount { get; init; }
}
