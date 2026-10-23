using BackpackingTracker.Core.Gear;
using BackpackingTracker.Core.Weight;

namespace BackpackingTracker.Core.Tests;

public class WeightCalculatorTests
{
    [Fact]
    public void No_entries_weigh_nothing()
    {
        var weight = WeightCalculator.Calculate([]);

        Assert.Equal((0m, 0m, 0m), (weight.BaseGrams, weight.WornGrams, weight.ConsumableGrams));
        Assert.Equal(0m, weight.StartingPackGrams);
    }

    [Fact]
    public void Entries_add_up_by_carry_mode()
    {
        var weight = WeightCalculator.Calculate(
        [
            new WeighedEntry(907m, 1, CarryMode.Base),
            new WeighedEntry(590m, 1, CarryMode.Base),
            new WeighedEntry(283m, 1, CarryMode.Worn),
            new WeighedEntry(110m, 1, CarryMode.Consumable),
        ]);

        Assert.Equal(1497m, weight.BaseGrams);
        Assert.Equal(283m, weight.WornGrams);
        Assert.Equal(110m, weight.ConsumableGrams);
    }

    [Fact]
    public void An_entry_weighs_its_item_weight_times_its_quantity()
    {
        var weight = WeightCalculator.Calculate(
        [
            new WeighedEntry(10.5m, 6, CarryMode.Base),
            new WeighedEntry(76m, 2, CarryMode.Worn),
        ]);

        Assert.Equal(63m, weight.BaseGrams);
        Assert.Equal(152m, weight.WornGrams);
    }

    [Fact]
    public void One_entry_weighs_its_item_weight_times_its_quantity()
    {
        Assert.Equal(63m, WeightCalculator.EntryWeight(new WeighedEntry(10.5m, 6, CarryMode.Base)));
    }

    [Fact]
    public void Starting_pack_weight_is_base_plus_consumable_and_leaves_out_worn()
    {
        var weight = WeightCalculator.Calculate(
        [
            new WeighedEntry(4000m, 1, CarryMode.Base),
            new WeighedEntry(1500m, 1, CarryMode.Worn),
            new WeighedEntry(220m, 2, CarryMode.Consumable),
        ]);

        Assert.Equal(4440m, weight.StartingPackGrams);
    }
}
