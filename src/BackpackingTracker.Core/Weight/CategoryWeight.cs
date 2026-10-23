using BackpackingTracker.Core.Lists;

namespace BackpackingTracker.Core.Weight;

/// <summary>One line of the Category breakdown: a Category's weight and its share (0 to 1) of the Starting Pack Weight.</summary>
public sealed record CategoryWeight(Category Category, decimal Grams, decimal Share);
