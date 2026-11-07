using BackpackingTracker.Core.Trips;

namespace BackpackingTracker.Web.Components;

/// <summary>How a Gear Change reads: in a summary ("+ Bear canister", "− Puffy") and on the checklist ("Pick up Bear canister").</summary>
public static class GearChangeText
{
    /// <summary>"+ Bear canister", "+ Bear canister ×2", "− Puffy", "Fuel canister ×1 → ×2".</summary>
    public static string Short(GearChangeView view) => view.Change.Kind switch
    {
        GearChangeKind.Add => $"+ {view.Name}{Times(view.Change.Quantity)}",
        GearChangeKind.Remove => $"− {view.Name}",
        _ => $"{view.Name} {From(view)}→ ×{view.Change.Quantity}",
    };

    /// <summary>"Pick up Bear canister ×2", "Send home Puffy", "Fuel canister ×1 → ×2".</summary>
    public static string Task(GearChangeView view) => view.Change.Kind switch
    {
        GearChangeKind.Add => $"Pick up {view.Name}{Times(view.Change.Quantity)}",
        GearChangeKind.Remove => $"Send home {view.Name}",
        _ => $"{view.Name} {From(view)}→ ×{view.Change.Quantity}",
    };

    private static string Times(int quantity) => quantity == 1 ? "" : $" ×{quantity}";

    private static string From(GearChangeView view) => view.FromQuantity is { } from ? $"×{from} " : "";
}
