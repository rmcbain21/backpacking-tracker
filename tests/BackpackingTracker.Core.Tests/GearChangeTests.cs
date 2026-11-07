using BackpackingTracker.Core.Food;
using BackpackingTracker.Core.Gear;
using BackpackingTracker.Core.Tests.Fakes;
using BackpackingTracker.Core.Trips;

namespace BackpackingTracker.Core.Tests;

/// <summary>Gear Changes at a Resupply: the gear each Food Carry carries, its pack weight, the checklist ticks and the locks.</summary>
public class GearChangeTests
{
    private readonly InMemoryRecordStore _store = new();

    // Midday on 10 July.
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 7, 10, 12, 0, 0, TimeSpan.Zero));
    private readonly AppCore _app;

    public GearChangeTests()
    {
        _app = new AppCore(_store, _clock);
    }

    private static DateOnly July(int day) => new(2026, 7, day);

    private async Task<TripView> ViewAsync(string tripId) =>
        await new AppCore(_store, _clock).Trips.GetTripAsync(tripId) ?? throw new InvalidOperationException("No such Trip.");

    private async Task<IReadOnlyList<FoodCarry>> CarriesAsync(string tripId) => (await ViewAsync(tripId)).MealPlan.FoodCarries;

    private static IEnumerable<(string, int)> Gear(FoodCarry carry) =>
        carry.Gear.Entries.Select(entry => (entry.Name, entry.Quantity));

    private sealed record Kit(Trip Trip, GearItem Tent, GearItem Puffy, GearItem Fuel, GearItem Shoes, GearItem BearCan, string Shelter);

    /// <summary>
    /// A Trip from 1 to 9 July with Resupplies at Kennedy Meadows (day 5) and Lone Pine (day 8), 1 L of water, a Clif Bar
    /// (68 g) every day, and a Packing List of Tent 1,000 g, Puffy 300 g, Fuel canister 200 g (consumable) and Trail
    /// runners 500 g (worn). A Bear canister (900 g) is in the catalogue but not packed.
    /// </summary>
    private async Task<Kit> SetUpAsync()
    {
        var trip = await _app.Trips.CreateTripAsync("PCT", July(1), July(9));
        var categories = (await ViewAsync(trip.Id)).Trip.PackingList.Categories;
        var shelter = categories[0].Id;

        async Task<GearItem> PackAsync(string name, decimal grams, CarryMode mode = CarryMode.Base)
        {
            var item = await _app.Gear.AddGearItemAsync(new GearItemDetails(name, null, grams));
            var entry = await _app.Trips.AddEntryAsync(trip.Id, item.Id, shelter);
            if (mode != CarryMode.Base)
            {
                await _app.Trips.UpdateEntryAsync(trip.Id, entry.Id, 1, mode);
            }

            return item;
        }

        var tent = await PackAsync("Tent", 1000m);
        var puffy = await PackAsync("Puffy", 300m);
        var fuel = await PackAsync("Fuel canister", 200m, CarryMode.Consumable);
        var shoes = await PackAsync("Trail runners", 500m, CarryMode.Worn);
        var bearCan = await _app.Gear.AddGearItemAsync(new GearItemDetails("Bear canister", null, 900m));
        await _app.Trips.SetWaterCarriedAsync(trip.Id, 1000m);
        var bar = await _app.Food.AddFoodItemAsync(new FoodItemDetails("Clif Bar", 68m, 260m));
        for (var day = 1; day <= 9; day++)
        {
            await _app.Trips.AddFoodItemToSlotAsync(trip.Id, July(day), MealSlot.Snacks, bar.Id);
        }

        await _app.Trips.SetResupplyAsync(trip.Id, July(5), true, "Kennedy Meadows");
        await _app.Trips.SetResupplyAsync(trip.Id, July(8), true, "Lone Pine");
        return new Kit(trip, tent, puffy, fuel, shoes, bearCan, shelter);
    }

    // ----- The gear in each Food Carry -----

    [Fact]
    public async Task Without_gear_changes_every_food_carry_carries_the_packing_list()
    {
        var kit = await SetUpAsync();

        var view = await ViewAsync(kit.Trip.Id);
        var carries = view.MealPlan.FoodCarries;

        Assert.All(carries, carry => Assert.Equal(1300m, carry.Gear.BaseGrams));
        Assert.Equal(view.Weight.StartingPackGrams, carries[0].PackOutGrams);
        // Base 1,300 + fuel 200 + water 1,000 + 3 days of food at Kennedy Meadows.
        Assert.Equal(2500m + 3 * 68m, carries[1].PackOutGrams);
    }

    [Fact]
    public async Task Gear_changes_carry_on_into_later_food_carries()
    {
        var kit = await SetUpAsync();

        await _app.Trips.AddGearPickUpAsync(kit.Trip.Id, July(5), kit.BearCan.Id, kit.Shelter);
        await _app.Trips.SendGearHomeAsync(kit.Trip.Id, July(5), kit.Puffy.Id);
        await _app.Trips.ChangeGearQuantityAsync(kit.Trip.Id, July(8), kit.Fuel.Id, 2);
        await _app.Trips.SendGearHomeAsync(kit.Trip.Id, July(8), kit.BearCan.Id);

        var carries = await CarriesAsync(kit.Trip.Id);
        Assert.Equal([("Tent", 1), ("Puffy", 1), ("Fuel canister", 1), ("Trail runners", 1)], Gear(carries[0]));
        Assert.Equal([("Tent", 1), ("Fuel canister", 1), ("Trail runners", 1), ("Bear canister", 1)], Gear(carries[1]));
        Assert.Equal([("Tent", 1), ("Fuel canister", 2), ("Trail runners", 1)], Gear(carries[2]));
        Assert.Equal((1900m, 200m), (carries[1].Gear.BaseGrams, carries[1].Gear.ConsumableGrams));
        Assert.Equal((1000m, 400m), (carries[2].Gear.BaseGrams, carries[2].Gear.ConsumableGrams));
        Assert.Equal(1, carries[2].Gear.Changes.Single(c => c.Change.Kind == GearChangeKind.ChangeQuantity).FromQuantity);
    }

    [Fact]
    public async Task Starting_pack_weight_stays_the_trailhead_figure()
    {
        var kit = await SetUpAsync();
        var before = (await ViewAsync(kit.Trip.Id)).Weight.StartingPackGrams;

        await _app.Trips.AddGearPickUpAsync(kit.Trip.Id, July(5), kit.BearCan.Id, kit.Shelter);

        Assert.Equal(before, (await ViewAsync(kit.Trip.Id)).Weight.StartingPackGrams);
    }

    [Fact]
    public async Task A_change_that_no_longer_applies_is_flagged_and_ignored()
    {
        var kit = await SetUpAsync();
        await _app.Trips.SendGearHomeAsync(kit.Trip.Id, July(8), kit.Puffy.Id);
        var puffyEntry = (await ViewAsync(kit.Trip.Id)).Trip.PackingList.Entries.Single(e => e.GearItemId == kit.Puffy.Id);

        await _app.Trips.RemoveEntryAsync(kit.Trip.Id, puffyEntry.Id);

        var change = Assert.Single((await CarriesAsync(kit.Trip.Id))[2].Gear.Changes);
        Assert.Equal("Puffy isn't carried by day 8", change.Problem);
        Assert.Equal(1000m, (await CarriesAsync(kit.Trip.Id))[2].Gear.BaseGrams);
    }

    [Fact]
    public async Task The_heaviest_pack_is_in_the_stats_once_there_is_a_resupply()
    {
        var kit = await SetUpAsync();
        await _app.Trips.AddGearPickUpAsync(kit.Trip.Id, July(5), kit.BearCan.Id, kit.Shelter);

        var heaviest = (await ViewAsync(kit.Trip.Id)).Stats.HeaviestPack;

        Assert.Equal("Kennedy Meadows", heaviest?.Place);
        Assert.Equal(3400m + 3 * 68m, heaviest?.PackOutGrams);
    }

    // ----- Making changes -----

    [Fact]
    public async Task Picking_the_same_gear_up_again_adds_to_it_and_unticks_it()
    {
        var kit = await SetUpAsync();
        var first = await _app.Trips.AddGearPickUpAsync(kit.Trip.Id, July(5), kit.BearCan.Id, kit.Shelter);
        await _app.Trips.SetGearChangeDoneAsync(kit.Trip.Id, July(5), first.Id, true);

        await _app.Trips.AddGearPickUpAsync(kit.Trip.Id, July(5), kit.BearCan.Id, kit.Shelter);

        var change = Assert.Single((await ViewAsync(kit.Trip.Id)).Trip.Days[4].GearChanges);
        Assert.Equal((2, false), (change.Quantity, change.Done));
    }

    [Fact]
    public async Task There_is_one_change_per_gear_item_at_a_resupply()
    {
        var kit = await SetUpAsync();
        await _app.Trips.ChangeGearQuantityAsync(kit.Trip.Id, July(5), kit.Fuel.Id, 3);

        await _app.Trips.SendGearHomeAsync(kit.Trip.Id, July(5), kit.Fuel.Id);
        await _app.Trips.AddGearPickUpAsync(kit.Trip.Id, July(5), kit.BearCan.Id, kit.Shelter);

        var changes = (await ViewAsync(kit.Trip.Id)).Trip.Days[4].GearChanges;
        Assert.Equal([GearChangeKind.Remove, GearChangeKind.Add], changes.Select(c => c.Kind));
        await Assert.ThrowsAsync<RuleViolationException>(() =>
            _app.Trips.AddGearPickUpAsync(kit.Trip.Id, July(5), kit.Fuel.Id, kit.Shelter));
        await Assert.ThrowsAsync<RuleViolationException>(() =>
            _app.Trips.SendGearHomeAsync(kit.Trip.Id, July(5), kit.BearCan.Id));
    }

    [Fact]
    public async Task Only_gear_carried_by_then_can_be_sent_home_or_changed()
    {
        var kit = await SetUpAsync();

        await Assert.ThrowsAsync<RuleViolationException>(() => _app.Trips.SendGearHomeAsync(kit.Trip.Id, July(5), kit.BearCan.Id));
        await Assert.ThrowsAsync<RuleViolationException>(() => _app.Trips.ChangeGearQuantityAsync(kit.Trip.Id, July(5), kit.Fuel.Id, 1));
        await Assert.ThrowsAsync<RuleViolationException>(() => _app.Trips.ChangeGearQuantityAsync(kit.Trip.Id, July(5), kit.Fuel.Id, 0));
    }

    [Fact]
    public async Task Gear_only_changes_at_a_resupply_and_not_to_retired_gear()
    {
        var kit = await SetUpAsync();
        var old = await _app.Gear.AddGearItemAsync(new GearItemDetails("Old can", null, 1100m));
        await _app.Gear.RetireGearItemAsync(old.Id);

        await Assert.ThrowsAsync<RuleViolationException>(() =>
            _app.Trips.AddGearPickUpAsync(kit.Trip.Id, July(6), kit.BearCan.Id, kit.Shelter));
        await Assert.ThrowsAsync<RuleViolationException>(() =>
            _app.Trips.SendGearHomeAsync(kit.Trip.Id, July(6), kit.Puffy.Id));
        await Assert.ThrowsAsync<RuleViolationException>(() =>
            _app.Trips.AddGearPickUpAsync(kit.Trip.Id, July(5), old.Id, kit.Shelter));
        await Assert.ThrowsAsync<RuleViolationException>(() =>
            _app.Trips.AddGearPickUpAsync(kit.Trip.Id, July(5), kit.BearCan.Id, "no such category"));
    }

    [Fact]
    public async Task Changing_the_quantity_of_a_change_unticks_it()
    {
        var kit = await SetUpAsync();
        var pickUp = await _app.Trips.AddGearPickUpAsync(kit.Trip.Id, July(5), kit.BearCan.Id, kit.Shelter);
        await _app.Trips.SetGearChangeDoneAsync(kit.Trip.Id, July(5), pickUp.Id, true);

        await _app.Trips.UpdateGearChangeAsync(kit.Trip.Id, July(5), pickUp.Id, 1, CarryMode.Consumable);
        var sameQuantity = Assert.Single((await ViewAsync(kit.Trip.Id)).Trip.Days[4].GearChanges);
        await _app.Trips.UpdateGearChangeAsync(kit.Trip.Id, July(5), pickUp.Id, 2, CarryMode.Consumable);
        var moreOfIt = Assert.Single((await ViewAsync(kit.Trip.Id)).Trip.Days[4].GearChanges);

        Assert.Equal((CarryMode.Consumable, true), (sameQuantity.CarryMode, sameQuantity.Done));
        Assert.Equal((2, false), (moreOfIt.Quantity, moreOfIt.Done));
    }

    [Fact]
    public async Task Turning_off_a_resupply_removes_its_gear_changes()
    {
        var kit = await SetUpAsync();
        await _app.Trips.AddGearPickUpAsync(kit.Trip.Id, July(5), kit.BearCan.Id, kit.Shelter);

        await _app.Trips.SetResupplyAsync(kit.Trip.Id, July(5), false);

        Assert.Empty((await ViewAsync(kit.Trip.Id)).Trip.Days[4].GearChanges);
    }

    // ----- Completing, deleting and merging gear -----

    [Fact]
    public async Task Completing_freezes_picked_up_gear_and_locks_the_changes()
    {
        var kit = await SetUpAsync();
        var pickUp = await _app.Trips.AddGearPickUpAsync(kit.Trip.Id, July(5), kit.BearCan.Id, kit.Shelter);
        await _app.Trips.CompleteTripAsync(kit.Trip.Id);

        await _app.Gear.UpdateGearItemAsync(kit.BearCan.Id, new GearItemDetails("Bear canister", null, 1200m));

        Assert.Equal(1900m + 300m, (await CarriesAsync(kit.Trip.Id))[1].Gear.BaseGrams);
        await Assert.ThrowsAsync<RuleViolationException>(() =>
            _app.Trips.SetGearChangeDoneAsync(kit.Trip.Id, July(5), pickUp.Id, true));
        await _app.Trips.ReopenTripAsync(kit.Trip.Id);
        Assert.Equal(2200m + 300m, (await CarriesAsync(kit.Trip.Id))[1].Gear.BaseGrams);
    }

    [Fact]
    public async Task Gear_picked_up_at_a_resupply_cant_be_deleted()
    {
        var kit = await SetUpAsync();
        await _app.Trips.AddGearPickUpAsync(kit.Trip.Id, July(5), kit.BearCan.Id, kit.Shelter);

        var plan = await _app.Gear.PlanGearItemDeletionAsync(kit.BearCan.Id);

        Assert.False(plan.CanDelete);
        Assert.Contains("Trip \"PCT\"", plan.UsedBy);
    }

    [Fact]
    public async Task Merging_moves_gear_changes_onto_the_kept_gear()
    {
        var kit = await SetUpAsync();
        await _app.Trips.AddGearPickUpAsync(kit.Trip.Id, July(5), kit.BearCan.Id, kit.Shelter);
        var other = await _app.Gear.AddGearItemAsync(new GearItemDetails("Bear canister", null, 905m));
        var group = Assert.Single(await _app.Duplicates.ListAsync());

        await _app.Duplicates.MergeAsync(group.GearItemIds, other.Id);

        Assert.Equal(other.Id, Assert.Single((await ViewAsync(kit.Trip.Id)).Trip.Days[4].GearChanges).GearItemId);
    }
}
