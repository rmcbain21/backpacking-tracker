using BackpackingTracker.Core.Gear;
using BackpackingTracker.Core.Units;
using BackpackingTracker.Core.Weight;

namespace BackpackingTracker.Core.Tests;

public class CategoryBreakdownTests
{
    private static Category NewCategory(string name, int sortOrder) =>
        new() { Id = $"{name}-id", UpdatedAt = DateTimeOffset.UnixEpoch, Name = name, SortOrder = sortOrder };

    private static readonly Category Shelter = NewCategory("Shelter", 0);
    private static readonly Category Sleep = NewCategory("Sleep", 1);
    private static readonly Category Cook = NewCategory("Cook", 3);
    private static readonly Category Clothing = NewCategory("Clothing", 5);
    private static readonly Category[] Categories = [Clothing, Cook, Shelter, Sleep];

    private static (string Category, decimal Grams)[] Lines(PackWeight weight) =>
        weight.ByCategory.Select(line => (line.Category.Name, line.Grams)).ToArray();

    [Fact]
    public void The_starting_pack_weight_is_split_by_category_in_category_order()
    {
        var weight = WeightCalculator.Calculate(
        [
            new WeighedEntry(900m, 1, CarryMode.Base, Shelter.Id),
            new WeighedEntry(100m, 1, CarryMode.Base, Cook.Id),
            new WeighedEntry(200m, 1, CarryMode.Worn, Clothing.Id),
            new WeighedEntry(100m, 1, CarryMode.Consumable, Cook.Id),
        ], Categories);

        Assert.Equal([("Shelter", 900m), ("Cook", 200m)], Lines(weight));
        Assert.Equal(900m / 1100m, weight.ByCategory[0].Share);
        Assert.Equal(200m / 1100m, weight.ByCategory[1].Share);
    }

    [Fact]
    public void Worn_entries_are_left_out_of_the_breakdown()
    {
        var weight = WeightCalculator.Calculate(
        [
            new WeighedEntry(200m, 1, CarryMode.Worn, Clothing.Id),
            new WeighedEntry(300m, 1, CarryMode.Base, Clothing.Id),
            new WeighedEntry(590m, 1, CarryMode.Base, Sleep.Id),
        ], Categories);

        Assert.Equal([("Sleep", 590m), ("Clothing", 300m)], Lines(weight));
    }

    [Fact]
    public void A_category_whose_entries_are_all_worn_is_not_listed()
    {
        var weight = WeightCalculator.Calculate(
        [
            new WeighedEntry(200m, 1, CarryMode.Worn, Clothing.Id),
            new WeighedEntry(900m, 1, CarryMode.Base, Shelter.Id),
        ], Categories);

        Assert.Equal([("Shelter", 900m)], Lines(weight));
    }

    [Fact]
    public void Quantities_count_towards_a_category()
    {
        var weight = WeightCalculator.Calculate([new WeighedEntry(10.5m, 6, CarryMode.Base, Shelter.Id)], Categories);

        Assert.Equal([("Shelter", 63m)], Lines(weight));
        Assert.Equal(1m, weight.ByCategory[0].Share);
    }

    [Fact]
    public void Shares_add_up_to_the_whole_pack()
    {
        var weight = WeightCalculator.Calculate(
        [
            new WeighedEntry(907m, 1, CarryMode.Base, Shelter.Id),
            new WeighedEntry(590m, 1, CarryMode.Base, Sleep.Id),
            new WeighedEntry(110m, 3, CarryMode.Consumable, Cook.Id),
        ], Categories);

        Assert.Equal(1m, weight.ByCategory.Sum(line => line.Share), precision: 20);
        Assert.Equal(weight.StartingPackGrams, weight.ByCategory.Sum(line => line.Grams));
    }

    [Fact]
    public void Entries_whose_category_no_longer_exists_go_last_under_no_category()
    {
        var weight = WeightCalculator.Calculate(
        [
            new WeighedEntry(12m, 1, CarryMode.Base, "removed-on-another-device"),
            new WeighedEntry(900m, 1, CarryMode.Base, Shelter.Id),
        ], Categories);

        Assert.Equal([("Shelter", 900m), ("No Category", 12m)], Lines(weight));
    }

    [Fact]
    public void An_empty_pack_has_no_breakdown()
    {
        Assert.Empty(WeightCalculator.Calculate([], Categories).ByCategory);
    }

    [Theory]
    [InlineData(0.818181, "82%")]
    [InlineData(0.181818, "18%")]
    [InlineData(1, "100%")]
    [InlineData(0.004, "0%")]
    [InlineData(0.005, "1%")]
    public void Shares_are_shown_as_whole_percentages(decimal share, string expected)
    {
        Assert.Equal(expected, UnitFormat.Share(share));
    }
}
