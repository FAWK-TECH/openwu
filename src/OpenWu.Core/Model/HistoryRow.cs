using System;

namespace OpenWu.Core.Model;

public sealed class HistoryRow
{
    public DateTime Date { get; init; }
    public required string Kb { get; init; }
    public required string Title { get; init; }
    public required string Result { get; init; }
    public required string UpdateId { get; init; }
}
