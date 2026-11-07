using BackpackingTracker.Core.Gear;

namespace BackpackingTracker.Core.Trips;

/// <summary>A Gear Item carried in a Food Carry: how many, how, under which Category, and what one weighs (0 if it's gone).</summary>
public sealed record CarriedEntry(string GearItemId, GearItem? GearItem, int Quantity, CarryMode CarryMode, string CategoryId, decimal ItemWeightGrams)
{
    public decimal WeightGrams => ItemWeightGrams * Quantity;

    public string Name => GearItem?.Name ?? "Deleted Gear Item";
}

/// <summary>
/// A Gear Change with its Gear Item looked up. FromQuantity is how many were carried before a ChangeQuantity. Problem
/// says why a change no longer applies ("Puffy isn't carried by day 15"); such a change is kept but left out of weights.
/// </summary>
public sealed record GearChangeView(GearChange Change, GearItem? GearItem, int? FromQuantity, string? Problem)
{
    public string Name => GearItem?.Name ?? "Deleted Gear Item";

    public bool Applies => Problem is null;
}

/// <summary>
/// The gear carried in one Food Carry: the trailhead Packing List plus every Gear Change up to and including the Food
/// Carry's Resupply, and that Resupply's own changes. Worn gear is listed but isn't in the pack, so it's left out of
/// the weights. Water carried is the trailhead figure, the same for every Food Carry.
/// </summary>
public sealed record CarriedGear(IReadOnlyList<CarriedEntry> Entries, IReadOnlyList<GearChangeView> Changes, decimal WaterGrams)
{
    public static CarriedGear None { get; } = new([], [], 0m);

    public decimal BaseGrams => Entries.Where(entry => entry.CarryMode == CarryMode.Base).Sum(entry => entry.WeightGrams);

    /// <summary>The consumable gear entries, such as fuel; food and water are counted separately.</summary>
    public decimal ConsumableGrams => Entries.Where(entry => entry.CarryMode == CarryMode.Consumable).Sum(entry => entry.WeightGrams);

    public int DoneCount => Changes.Count(change => change.Change.Done);

    /// <summary>
    /// Each Food Carry's gear, in order: the first carries the Packing List; each later one starts from the one before
    /// and applies its Resupply day's Gear Changes in order. An Add of gear already carried adds to its quantity; a
    /// Remove or ChangeQuantity of gear not carried is flagged and skipped.
    /// </summary>
    internal static IReadOnlyList<CarriedGear> For(Trip trip, IReadOnlyList<FoodCarry> carries, IReadOnlyDictionary<string, GearItem> items)
    {
        var water = trip.WaterCarriedGrams ?? 0m;
        var carried = trip.PackingList.Entries
            .Select(entry =>
            {
                var item = items.GetValueOrDefault(entry.GearItemId);
                return new CarriedEntry(entry.GearItemId, item, entry.Quantity, entry.CarryMode, entry.CategoryId,
                    entry.FrozenWeightGrams ?? item?.WeightGrams ?? 0m);
            })
            .ToList();

        var result = new List<CarriedGear>();
        foreach (var carry in carries)
        {
            var changes = new List<GearChangeView>();
            if (!carry.FromHome)
            {
                foreach (var change in carry.FirstDay.GearChanges)
                {
                    var (view, next) = Apply(change, carried, items, carry.FirstDayNumber);
                    changes.Add(view);
                    carried = next;
                }
            }

            result.Add(new CarriedGear(carried, changes, water));
        }

        return result;
    }

    private static (GearChangeView View, List<CarriedEntry> Carried) Apply(
        GearChange change, List<CarriedEntry> carried, IReadOnlyDictionary<string, GearItem> items, int dayNumber)
    {
        var item = items.GetValueOrDefault(change.GearItemId);
        var index = carried.FindIndex(entry => entry.GearItemId == change.GearItemId);
        var next = carried.ToList();
        if (change.Kind == GearChangeKind.Add)
        {
            if (index >= 0)
            {
                next[index] = next[index] with { Quantity = next[index].Quantity + change.Quantity };
            }
            else
            {
                next.Add(new CarriedEntry(change.GearItemId, item, change.Quantity, change.CarryMode, change.CategoryId ?? "",
                    change.FrozenWeightGrams ?? item?.WeightGrams ?? 0m));
            }

            return (new GearChangeView(change, item, null, null), next);
        }

        if (index < 0)
        {
            var name = item?.Name ?? "That Gear Item";
            return (new GearChangeView(change, item, null, $"{name} isn't carried by day {dayNumber}"), carried);
        }

        var from = carried[index].Quantity;
        if (change.Kind == GearChangeKind.Remove)
        {
            next.RemoveAt(index);
        }
        else
        {
            next[index] = next[index] with { Quantity = change.Quantity };
        }

        return (new GearChangeView(change, item, from, null), next);
    }
}
