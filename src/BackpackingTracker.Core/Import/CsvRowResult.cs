using BackpackingTracker.Core.Gear;

namespace BackpackingTracker.Core.Import;

/// <summary>
/// What happened to one CSV row. RowNumber counts the header as row 1, as a spreadsheet does.
/// GearItem is the created or matched Gear Item (null when Rejected); Reason is set only when Rejected.
/// Quantity and CarryMode are what the row asked for, for creating a Gear List (ticket 11).
/// </summary>
public sealed record CsvRowResult(
    int RowNumber,
    CsvRowOutcome Outcome,
    string Name,
    GearItem? GearItem = null,
    int Quantity = 1,
    CarryMode CarryMode = CarryMode.Base,
    decimal? CsvWeightGrams = null,
    string? Reason = null);
