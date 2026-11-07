using System.Text.Json.Serialization;

namespace BackpackingTracker.Core.Trips;

/// <summary>
/// One calendar day of a Trip: its route, journal notes and meal slots, all optional. Distances and elevation
/// gain are stored in metres (ADR 0002). Kept inside the Trip record, in date order, one per day of the Trip.
/// </summary>
public sealed record TripDay
{
    public required DateOnly Date { get; init; }

    /// <summary>Where the day starts, such as a trailhead.</summary>
    public string? Start { get; init; }

    /// <summary>Where the day ends.</summary>
    public string? End { get; init; }

    public string? Campsite { get; init; }

    public decimal? PlannedDistanceMetres { get; init; }

    public decimal? ActualDistanceMetres { get; init; }

    public decimal? ElevationGainMetres { get; init; }

    public string? JournalNotes { get; init; }

    /// <summary>
    /// Planned but not hiked, such as a first day lost to a late arrival. Set once the Trip has started instead of
    /// changing its dates, so the plan is kept; stats leave the day out, except its planned distance and its food.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool Skipped { get; init; }

    /// <summary>
    /// Food is restocked before this day's hiking, so a new Food Carry starts here. Never on day 1 (a Resupply that
    /// ends up on day 1 after a date change is ignored). Locks with the Meal Plan, since it changes Starting Pack Weight.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool Resupply { get; init; }

    /// <summary>Where a Resupply is, such as "Kennedy Meadows"; null when blank (the day's start is shown instead).</summary>
    public string? ResupplyPlace { get; init; }

    /// <summary>
    /// On the first day of a Food Carry (day 1 or a Resupply day): the shopping list lines ticked as bought, each
    /// with the servings it had when ticked.
    /// </summary>
    public IReadOnlyList<BoughtFood> Bought { get; init; } = [];

    /// <summary>
    /// On the first day of a Food Carry: its "To choose at the store" lines ticked as bought, each with its quantity when
    /// ticked.
    /// </summary>
    public IReadOnlyList<BoughtPlaceholder> BoughtPlaceholders { get; init; } = [];

    /// <summary>The day's Meal Plan: what's in its Breakfast, Lunch, Dinner and Snacks slots, in the order added.</summary>
    public IReadOnlyList<SlotEntry> SlotEntries { get; init; } = [];

    /// <summary>True when anything has been filled in, planned or marked, so removing the day would lose it.</summary>
    public bool HasContent() =>
        Start is not null || End is not null || Campsite is not null ||
        PlannedDistanceMetres is not null || ActualDistanceMetres is not null || ElevationGainMetres is not null ||
        JournalNotes is not null || SlotEntries.Count > 0 || Skipped || Resupply;

    /// <summary>Compares the slot entries and bought ticks one by one, so two days with the same plan are equal.</summary>
    public bool Equals(TripDay? other) =>
        other is not null &&
        (Date, Start, End, Campsite, PlannedDistanceMetres, ActualDistanceMetres, ElevationGainMetres, JournalNotes, Skipped) ==
        (other.Date, other.Start, other.End, other.Campsite, other.PlannedDistanceMetres, other.ActualDistanceMetres, other.ElevationGainMetres, other.JournalNotes, other.Skipped) &&
        (Resupply, ResupplyPlace) == (other.Resupply, other.ResupplyPlace) &&
        SlotEntries.SequenceEqual(other.SlotEntries) &&
        Bought.SequenceEqual(other.Bought) &&
        BoughtPlaceholders.SequenceEqual(other.BoughtPlaceholders);

    public override int GetHashCode() => HashCode.Combine(Date, Start, End, Campsite, JournalNotes, SlotEntries.Count);
}

/// <summary>A Food Item ticked as bought on a Food Carry's shopping list, with the servings the line had then.</summary>
public sealed record BoughtFood(string FoodItemId, decimal Servings);

/// <summary>A "To choose at the store" line ticked as bought: its slot and label, and the quantity it had then.</summary>
public sealed record BoughtPlaceholder(MealSlot Slot, string? Label, decimal Quantity)
{
    /// <summary>The same line: the same slot and label, ignoring case.</summary>
    public bool Matches(MealSlot slot, string? label) =>
        Slot == slot && string.Equals(Label, label, StringComparison.OrdinalIgnoreCase);
}

/// <summary>What can be typed in on a Trip Day. Blank text and missing numbers leave a field empty.</summary>
public sealed record TripDayDetails(
    string? Start = null,
    string? End = null,
    string? Campsite = null,
    decimal? PlannedDistanceMetres = null,
    decimal? ActualDistanceMetres = null,
    decimal? ElevationGainMetres = null,
    string? JournalNotes = null);
