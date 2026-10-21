using Microsoft.AspNetCore.Components.Routing;
using MudBlazor;

namespace BackpackingTracker.Web.Layout;

/// <summary>A top-level destination, shared by the bottom navigation and the drawer.</summary>
public sealed record NavItem(string Title, string Href, string Icon, NavLinkMatch Match)
{
    public static readonly IReadOnlyList<NavItem> All =
    [
        new("Gear", "", Icons.Material.Filled.Backpack, NavLinkMatch.All),
        new("Lists", "lists", Icons.Material.Filled.Checklist, NavLinkMatch.Prefix),
        new("Trips", "trips", Icons.Material.Filled.Terrain, NavLinkMatch.Prefix),
        new("Food", "food", Icons.Material.Filled.Restaurant, NavLinkMatch.Prefix),
    ];
}
