using BackpackingTracker.Core.Gear;

namespace BackpackingTracker.Core.Import;

/// <summary>
/// What happened to one CSV row. RowNumber counts the header as row 1, as a spreadsheet does.
/// Category is the row's Category (a trip sheet's section). GearItem is the created or matched Gear Item
/// (null when Rejected); Reason is set only when Rejected. Quantity and CarryMode are what the row asked
/// for, for creating a Gear List (ticket 29).
/// </summary>
public sealed record CsvRowResult(
    int RowNumber,
    CsvRowOutcome Outcome,
    string Name,
    string Category,
    GearItem? GearItem = null,
    int Quantity = 1,
    CarryMode CarryMode = CarryMode.Base,
    decimal? CsvWeightGrams = null,
    string? Reason = null);
