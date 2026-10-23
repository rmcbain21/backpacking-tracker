using BackpackingTracker.Core.Lists;

namespace BackpackingTracker.Core.Tests.Fakes;

/// <summary>For tests where it doesn't matter which Category an entry is in.</summary>
public static class GearListsTestExtensions
{
    /// <summary>Adds the Gear Item into the list's first Category.</summary>
    public static async Task<ListEntry> AddEntryAsync(this GearLists lists, string gearListId, string gearItemId)
    {
        var list = await lists.GetGearListAsync(gearListId) ?? throw new InvalidOperationException("No such Gear List.");
        return await lists.AddEntryAsync(gearListId, gearItemId, list.GearList.Categories[0].Id);
    }
}
