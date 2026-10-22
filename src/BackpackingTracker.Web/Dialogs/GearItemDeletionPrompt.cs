using BackpackingTracker.Core.Gear;
using MudBlazor;

namespace BackpackingTracker.Web.Dialogs;

public static class GearItemDeletionPrompt
{
    /// <summary>
    /// Shows the deletion plan's message. Returns true if the backpacker chose the plan's
    /// confirm button (Delete, or Retire instead); a plan with nothing to do just shows OK.
    /// </summary>
    public static async Task<bool> ConfirmGearItemDeletionAsync(this IDialogService dialogService, GearItemDeletionPlan plan)
    {
        if (plan.ConfirmButton is null)
        {
            await dialogService.ShowMessageBoxAsync(plan.Title, plan.Message, yesText: "OK");
            return false;
        }

        var confirmed = await dialogService.ShowMessageBoxAsync(
            plan.Title, plan.Message, yesText: plan.ConfirmButton, cancelText: "Cancel");
        return confirmed == true;
    }
}
