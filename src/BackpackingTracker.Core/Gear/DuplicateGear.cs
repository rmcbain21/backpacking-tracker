using BackpackingTracker.Core.Lists;
using BackpackingTracker.Core.Ports;
using BackpackingTracker.Core.Sync;
using BackpackingTracker.Core.Trips;

namespace BackpackingTracker.Core.Gear;

/// <summary>
/// Finds duplicate Gear Items (same Gear Type and Name, which sync can't tell apart because they have different
/// ids) and Merges them: one is kept as it is, everything using the others uses it instead, and the others are
/// deleted, each tombstone remembering the Gear Item it was merged into (ADR 0009).
/// </summary>
public sealed class DuplicateGear
{
    internal const string NotDuplicatesTable = "notDuplicates";

    private readonly IRecordStore _store;
    private readonly IClock _clock;
    private readonly GearCatalogue _gear;
    private readonly GearLists _lists;
    private readonly TripPlanner _trips;
    private readonly CloudSync _sync;

    // Set when a sync or import brings in Gear Items, until the backpacker reviews or closes the notice. Only for
    // this session: the review is always there from the Gear page's menu.
    private bool _reviewSuggested;

    public DuplicateGear(IRecordStore store, IClock clock, GearCatalogue gear, GearLists lists, TripPlanner trips, CloudSync sync)
    {
        _store = store;
        _clock = clock;
        _gear = gear;
        _lists = lists;
        _trips = trips;
        _sync = sync;
    }

    /// <summary>
    /// Every set of duplicate Gear Items, Retired ones included, by Gear Type then Name, leaving out sets marked
    /// as not duplicates. A set with a Gear Item in a Sync Conflict is listed but can't be merged until it's settled.
    /// </summary>
    public async Task<IReadOnlyList<DuplicateGroup>> ListAsync()
    {
        var dismissed = (await _store.ListAsync<NotDuplicates>(NotDuplicatesTable))
            .Where(record => !record.Deleted)
            .Select(record => record.Id)
            .ToHashSet();

        var groups = new List<DuplicateGroup>();
        foreach (var items in await FindMatchingAsync())
        {
            if (!dismissed.Contains(NotDuplicates.IdFor(items.Select(item => item.Id))))
            {
                groups.Add(await DescribeAsync(items));
            }
        }

        return groups;
    }

    /// <summary>
    /// How many sets of duplicates to tell the backpacker about after a sync or import brought in Gear Items;
    /// 0 when there was no such sync, or the notice was closed.
    /// </summary>
    public async Task<int> CountToReviewAsync()
    {
        if (!_reviewSuggested)
        {
            return 0;
        }

        var count = (await ListAsync()).Count;
        _reviewSuggested = count > 0;
        return count;
    }

    /// <summary>The backpacker closed the notice, or opened the review.</summary>
    public void CloseReviewNotice() => _reviewSuggested = false;

    /// <summary>What merging these duplicates into the one to keep will change. Refuses as <see cref="MergeAsync"/> would.</summary>
    public async Task<GearMergePlan> PlanMergeAsync(IReadOnlyList<string> gearItemIds, string keepId)
    {
        var (kept, mergedInto) = await CheckMergeAsync(gearItemIds, keepId);
        return new GearMergePlan(
            kept, mergedInto.Count, (await _lists.RepointAsync(mergedInto)).Count, (await _trips.RepointAsync(mergedInto)).Count);
    }

    /// <summary>
    /// Merges the duplicates into the one to keep, which stays as it is. Every Gear List and Packing List, a
    /// Completed Trip's too, uses it instead of the others (see <see cref="ListContents.Repoint"/>), and the
    /// others are deleted. Saved in one go. Refuses if they're no longer exactly a set of duplicates, or one is
    /// in a Sync Conflict.
    /// </summary>
    public async Task MergeAsync(IReadOnlyList<string> gearItemIds, string keepId)
    {
        var (_, mergedInto) = await CheckMergeAsync(gearItemIds, keepId);
        var items = await _gear.GetGearItemsAsync(mergedInto.Keys);

        var toSave = new List<RecordToSave>();
        toSave.AddRange(items.Values.Select(item => new RecordToSave(
            GearCatalogue.Table, item with { Deleted = true, MergedIntoId = keepId, UpdatedAt = _clock.UtcNow })));
        toSave.AddRange((await _lists.RepointAsync(mergedInto)).Select(list => new RecordToSave(GearLists.Table, list)));
        toSave.AddRange((await _trips.RepointAsync(mergedInto)).Select(trip => new RecordToSave(TripPlanner.Table, trip)));
        await _store.SaveAllAsync(toSave);
    }

    /// <summary>Marks the set as not duplicates, on every device, so it isn't offered again. A set with another Gear Item in it is.</summary>
    public async Task MarkNotDuplicatesAsync(IReadOnlyList<string> gearItemIds)
    {
        var ids = gearItemIds.Distinct().Order(StringComparer.Ordinal).ToList();
        await _store.SaveAsync(NotDuplicatesTable, new NotDuplicates
        {
            Id = NotDuplicates.IdFor(ids),
            UpdatedAt = _clock.UtcNow,
            GearItemIds = ids,
        });
    }

    /// <summary>
    /// After a sync or import saved records here: moves entries still using Gear Items merged away (a list
    /// changed on another device before it had the Merge) onto the Gear Items they were merged into, as a local
    /// change the next sync sends; and suggests a review if Gear Items arrived.
    /// </summary>
    internal async Task ChangesArrivedAsync(ChangeSummary summary)
    {
        var mergedInto = await _gear.ListMergedIntoAsync();
        if (mergedInto.Count > 0)
        {
            var toSave = new List<RecordToSave>();
            toSave.AddRange((await _lists.RepointAsync(mergedInto)).Select(list => new RecordToSave(GearLists.Table, list)));
            toSave.AddRange((await _trips.RepointAsync(mergedInto)).Select(trip => new RecordToSave(TripPlanner.Table, trip)));
            if (toSave.Count > 0)
            {
                await _store.SaveAllAsync(toSave);
            }
        }

        var gearItems = RecordTable.Named(GearCatalogue.Table).RecordTypeName;
        if (summary.Changes.Any(change => change.RecordType == gearItems && change.Added > 0))
        {
            _reviewSuggested = true;
        }
    }

    /// <summary>Every set of two or more matching Gear Items, by Gear Type then Name, dismissed or not.</summary>
    private async Task<IReadOnlyList<IReadOnlyList<GearItem>>> FindMatchingAsync() =>
        (await _gear.ListAllGearItemsAsync())
            .GroupBy(GearMatch.Key)
            .Where(group => group.Count() > 1)
            .Select(group => (IReadOnlyList<GearItem>)group.ToList())
            .ToList();

    private async Task<DuplicateGroup> DescribeAsync(IReadOnlyList<GearItem> items)
    {
        var candidates = new List<DuplicateCandidate>();
        foreach (var item in items)
        {
            candidates.Add(new DuplicateCandidate(item, (await _gear.PlanGearItemDeletionAsync(item.Id)).UsedBy));
        }

        var ordered = candidates
            .OrderByDescending(candidate => candidate.UsedBy.Count)
            .ThenBy(candidate => candidate.GearItem.Retired)
            .ToList();

        var conflicted = await _sync.ListConflictedIdsAsync(GearCatalogue.Table);
        var blocked = items.Any(item => conflicted.Contains(item.Id))
            ? "One of these is in a Sync Conflict. Settle its Sync Conflict first."
            : null;

        return new DuplicateGroup(ordered, ordered[0].GearItem.Id, blocked);
    }

    /// <summary>The Gear Item to keep and the ones to merge into it, if they're still exactly a set of duplicates.</summary>
    private async Task<(GearItem Kept, IReadOnlyDictionary<string, string> MergedInto)> CheckMergeAsync(
        IReadOnlyList<string> gearItemIds, string keepId)
    {
        var wanted = NotDuplicates.IdFor(gearItemIds.Distinct());
        var items = (await FindMatchingAsync()).FirstOrDefault(group => NotDuplicates.IdFor(group.Select(item => item.Id)) == wanted)
            ?? throw new RuleViolationException("These Gear Items have changed since you looked. Check them again.");

        var kept = items.FirstOrDefault(item => item.Id == keepId)
            ?? throw new RuleViolationException("Choose which of these Gear Items to keep.");

        if ((await DescribeAsync(items)).BlockedReason is { } blocked)
        {
            throw new RuleViolationException(blocked);
        }

        return (kept, items.Where(item => item.Id != keepId).ToDictionary(item => item.Id, _ => keepId));
    }
}
