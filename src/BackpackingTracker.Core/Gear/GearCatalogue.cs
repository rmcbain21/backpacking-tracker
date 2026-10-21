using BackpackingTracker.Core.Ports;

namespace BackpackingTracker.Core.Gear;

public sealed class GearCatalogue
{
    private const string Table = "gearItems";

    private readonly IRecordStore _store;
    private readonly IClock _clock;

    public GearCatalogue(IRecordStore store, IClock clock)
    {
        _store = store;
        _clock = clock;
    }

    public async Task<GearItem> AddGearItemAsync(string name, string brandModel, decimal weightGrams)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentOutOfRangeException.ThrowIfNegative(weightGrams);

        var item = new GearItem
        {
            Id = Guid.NewGuid().ToString(),
            UpdatedAt = _clock.UtcNow,
            Name = name.Trim(),
            BrandModel = brandModel.Trim(),
            WeightGrams = Math.Round(weightGrams, 1, MidpointRounding.AwayFromZero),
        };

        await _store.SaveAsync(Table, item);
        return item;
    }

    public async Task<IReadOnlyList<GearItem>> ListGearItemsAsync()
    {
        var items = await _store.ListAsync<GearItem>(Table);
        return items
            .OrderBy(item => item.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}
