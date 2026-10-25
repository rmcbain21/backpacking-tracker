using System.Globalization;
using BackpackingTracker.Core.Food;
using BackpackingTracker.Core.Gear;
using BackpackingTracker.Core.Lists;
using BackpackingTracker.Core.Ports;
using BackpackingTracker.Core.Preferences;
using BackpackingTracker.Core.Trips;
using BackpackingTracker.Core.Units;

namespace BackpackingTracker.Core.Sync;

/// <summary>Which version settles a Sync Conflict.</summary>
public enum SyncConflictChoice
{
    ThisDevice,
    OtherDevice,
}

/// <summary>
/// A Sync Conflict as the backpacker sees it: the record, and its two versions side by side in display units.
/// Small records list every line; Trips and Gear Lists list only the lines that differ, with
/// <see cref="SameCount"/> saying how many others are the same.
/// </summary>
public sealed record SyncConflictView(string Id, string RecordType, string Name, IReadOnlyList<ConflictLine> Lines, int SameCount);

/// <summary>One detail of a record, such as "Weight", in this device's version and the other device's.</summary>
public sealed record ConflictLine(string Label, string ThisDevice, string OtherDevice)
{
    public bool Differs => ThisDevice != OtherDevice;
}

/// <summary>Turns the two versions of a record into <see cref="ConflictLine"/>s, looking up the names of what they refer to.</summary>
internal sealed class ConflictDescriber
{
    private const string Empty = "—";
    private static readonly CultureInfo Culture = CultureInfo.InvariantCulture;

    private readonly UnitSystem _units;
    private readonly IReadOnlyDictionary<string, string> _names;

    private ConflictDescriber(UnitSystem units, IReadOnlyDictionary<string, string> names)
    {
        _units = units;
        _names = names;
    }

    public static async Task<ConflictDescriber> CreateAsync(IRecordStore store, UnitSystem units)
    {
        // Ids are GUIDs, so one lookup serves Gear Items, Food Items and Meals, deleted ones included.
        var names = new Dictionary<string, string>();
        foreach (var record in (await store.ListAsync<GearItem>(GearCatalogue.Table)).Cast<StoredRecord>()
                     .Concat(await store.ListAsync<FoodItem>(FoodCatalogue.Table))
                     .Concat(await store.ListAsync<Meal>(MealCatalogue.Table)))
        {
            names[record.Id] = NameOf(record);
        }

        return new ConflictDescriber(units, names);
    }

    public SyncConflictView Describe(string id, RecordTable table, StoredRecord mine, StoredRecord theirs)
    {
        var myLines = LinesOf(mine);
        var theirLines = LinesOf(theirs);
        var lines = myLines.Keys.Union(theirLines.Keys)
            .Select(label => new ConflictLine(label, myLines.GetValueOrDefault(label, Empty), theirLines.GetValueOrDefault(label, Empty)))
            .ToList();
        if (mine.Deleted || theirs.Deleted)
        {
            lines.Insert(0, new ConflictLine("Deleted", YesNo(mine.Deleted), YesNo(theirs.Deleted)));
        }

        var onlyDifferences = mine is Trip or GearList;
        return new SyncConflictView(
            id,
            table.RecordName,
            NameOf(mine.Deleted ? theirs : mine),
            onlyDifferences ? lines.Where(line => line.Differs).ToList() : lines,
            onlyDifferences ? lines.Count(line => !line.Differs) : 0);
    }

    private static string NameOf(StoredRecord record) => record switch
    {
        GearItem item => item.Name,
        FoodItem item => item.Name,
        Meal meal => meal.Name,
        GearList list => list.Name,
        Trip trip => trip.Name,
        DefaultCategory category => category.Name,
        _ => "Settings",
    };

    private Lines LinesOf(StoredRecord record)
    {
        var lines = new Lines();
        switch (record)
        {
            case GearItem item:
                lines.Put("Name", item.Name);
                lines.Put("Gear Type", item.GearType);
                lines.Put("Weight", Weight(item.WeightGrams));
                lines.Put("Tags", string.Join(", ", item.Tags));
                lines.Put("Price", Money(item.Price));
                lines.Put("Notes", item.Notes);
                lines.Put("Retired", YesNo(item.Retired));
                break;
            case FoodItem item:
                lines.Put("Name", item.Name);
                lines.Put("Serving weight", Weight(item.ServingWeightGrams));
                lines.Put("Calories per serving", $"{Number(item.CaloriesPerServing)} cal");
                lines.Put("Protein per serving", item.ProteinGramsPerServing is { } protein ? $"{Number(protein)} g" : null);
                lines.Put("Cost per serving", Money(item.CostPerServing));
                lines.Put("Tags", string.Join(", ", item.Tags));
                lines.Put("Retired", YesNo(item.Retired));
                break;
            case Meal meal:
                lines.Put("Name", meal.Name);
                lines.Put("Tags", string.Join(", ", meal.Tags));
                lines.Put("Retired", YesNo(meal.Retired));
                foreach (var component in meal.Components)
                {
                    lines.Put(NameFor(component.FoodItemId), $"{Number(component.Servings)} servings");
                }

                break;
            case DefaultCategory category:
                lines.Put("Name", category.Name);
                lines.Put("Position", (category.SortOrder + 1).ToString(Culture));
                break;
            case Settings settings:
                lines.Put("Units", settings.UnitSystem.ToString());
                lines.Put("Colour palette", PaletteName(settings.Palette));
                lines.Put("Appearance", settings.Appearance == Appearance.MatchDevice ? "Match my device" : settings.Appearance.ToString());
                lines.Put("Text size", settings.TextSize == TextSize.ExtraSmall ? "Extra small" : settings.TextSize.ToString());
                break;
            case GearList list:
                lines.Put("Name", list.Name);
                AddListContents(lines, list.Contents, prefix: "");
                break;
            case Trip trip:
                lines.Put("Name", trip.Name);
                lines.Put("Dates", $"{Date(trip.StartDate)} to {Date(trip.EndDate)}");
                lines.Put("Status", trip.Status.ToString());
                lines.Put("Created from", trip.CreatedFrom);
                lines.Put("Water carried", trip.WaterCarriedGrams is { } water ? UnitFormat.WaterVolume(water) : null);
                AddListContents(lines, trip.PackingList, prefix: "Packing List: ");
                for (var i = 0; i < trip.Days.Count; i++)
                {
                    AddDay(lines, $"Day {i + 1}", trip.Days[i]);
                }

                break;
        }

        return lines;
    }

    private void AddListContents(Lines lines, ListContents contents, string prefix)
    {
        lines.Put($"{prefix}Categories", string.Join(", ", contents.Categories.Select(category => category.Name)));
        var categoryNames = contents.Categories.ToDictionary(category => category.Id, category => category.Name);
        foreach (var entry in contents.Entries)
        {
            var parts = new List<string>
            {
                $"×{entry.Quantity}",
                entry.CarryMode.ToString().ToLowerInvariant(),
                categoryNames.GetValueOrDefault(entry.CategoryId, Category.None.Name),
            };
            if (entry.Packed)
            {
                parts.Add("packed");
            }

            lines.Put(prefix + NameFor(entry.GearItemId), string.Join(" · ", parts));
        }
    }

    private void AddDay(Lines lines, string day, TripDay tripDay)
    {
        lines.Put($"{day} start", tripDay.Start);
        lines.Put($"{day} end", tripDay.End);
        lines.Put($"{day} campsite", tripDay.Campsite);
        lines.Put($"{day} planned distance", tripDay.PlannedDistanceMetres is { } planned ? UnitFormat.Distance(planned, _units) : null);
        lines.Put($"{day} actual distance", tripDay.ActualDistanceMetres is { } actual ? UnitFormat.Distance(actual, _units) : null);
        lines.Put($"{day} elevation gain", tripDay.ElevationGainMetres is { } gain ? UnitFormat.Elevation(gain, _units) : null);
        lines.Put($"{day} notes", tripDay.JournalNotes);
        foreach (var slot in Enum.GetValues<MealSlot>())
        {
            var food = tripDay.SlotEntries
                .Where(entry => entry.Slot == slot)
                .Select(entry => $"{NameFor(entry.MealId ?? entry.FoodItemId ?? "")} ×{Number(entry.Quantity)}");
            lines.Put($"{day} {slot.ToString().ToLowerInvariant()}", string.Join(", ", food));
        }
    }

    private string NameFor(string id) => _names.GetValueOrDefault(id, "Unknown");

    private string Weight(decimal grams) => UnitFormat.Weight(grams, _units);

    private static string? Money(decimal? amount) => amount?.ToString("0.00", Culture);

    private static string Number(decimal value) => value.ToString("0.##", Culture);

    private static string Date(DateOnly date) => date.ToString("d MMM yyyy", Culture);

    private static string YesNo(bool value) => value ? "Yes" : "No";

    private static string PaletteName(ColourPalette palette) => palette switch
    {
        ColourPalette.AlpineLake => "Alpine Lake",
        ColourPalette.CanyonSandstone => "Canyon Sandstone",
        ColourPalette.MossAndLichen => "Moss & Lichen",
        ColourPalette.TwilightCampfire => "Twilight Campfire",
        _ => palette.ToString(),
    };

    /// <summary>A record's details by label, in order. Blank values show as a dash; a repeated label gets a number.</summary>
    private sealed class Lines : OrderedDictionary<string, string>
    {
        public void Put(string label, string? value)
        {
            var unique = label;
            for (var n = 2; ContainsKey(unique); n++)
            {
                unique = $"{label} ({n})";
            }

            Add(unique, string.IsNullOrWhiteSpace(value) ? Empty : value);
        }
    }
}
