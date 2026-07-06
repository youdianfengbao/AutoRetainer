using AutoRetainerAPI.Configuration;
using ECommons.Automation;
using ECommons.ExcelServices;
using ECommons.Throttlers;
using ECommons.WindowsFormsReflector;

namespace AutoRetainer.UI.NeoUI.InventoryManagementEntries.InventoryCleanupEntries;
public unsafe class FastAddition : InventoryManagementBase
{
    public override string Name { get; } = "物品清理/快速添加与移除";

    private FastAddition()
    {
        Builder = InventoryCleanupCommon.CreateCleanupHeaderBuilder()
        .Section(Name)
        .Widget(() =>
        {
            var selectedSettings = InventoryCleanupCommon.SelectedPlan;
            ImGuiEx.TextWrapped(GradientColor.Get(EColor.RedBright, EColor.YellowBright), $"当此文本可见时，按住以下按键悬停物品：");
            ImGuiEx.Text(!ImGui.GetIO().KeyShift ? ImGuiColors.DalamudGrey : ImGuiColors.DalamudRed, $"Shift - 添加到快速探险出售列表");
            ImGuiEx.Text($"* 已在无条件出售列表或丢弃列表中的物品将不会被添加到快速探险出售列表");
            ImGuiEx.Text(!ImGui.GetIO().KeyCtrl ? ImGuiColors.DalamudGrey : ImGuiColors.DalamudRed, $"Ctrl - 添加到无条件出售列表");
            ImGuiEx.Text($"* 已在其他列表中的物品将被移动到无条件出售列表");
            ImGuiEx.Text(!IsKeyPressed(Keys.Tab) ? ImGuiColors.DalamudGrey : ImGuiColors.DalamudRed, $"Tab - 添加到丢弃列表");
            ImGuiEx.Text($"* 已在其他列表中的物品将被移动到丢弃列表");
            //ImGuiEx.Text(IsKeyPressed(Keys.Space) ? ImGuiColors.DalamudGrey : ImGuiColors.DalamudRed, $"Space - add to Desynthesis List");
            //ImGuiEx.Text($"* Items that already in other lists WILL BE MOVED to Desynthesis List");
            ImGuiEx.Text(!ImGui.GetIO().KeyAlt ? ImGuiColors.DalamudGrey : ImGuiColors.DalamudRed, $"Alt - 从任何列表中删除");
            ImGuiEx.Text("\n受保护的物品不受这些操作影响");
            if(Svc.GameGui.HoveredItem > 0)
            {
                var id = (uint)(Svc.GameGui.HoveredItem % 1000000);
                if(ImGui.GetIO().KeyShift)
                {
                    if(!selectedSettings.IMProtectList.Contains(id) 
                    && !selectedSettings.IMAutoVendorSoft.Contains(id)
                    && !selectedSettings.IMAutoVendorHard.Contains(id)
                    && !selectedSettings.IMDiscardList.Contains(id)
                    && !selectedSettings.IMDesynth.Contains(id)
                    )
                    {
                        if(selectedSettings.AddItemToList(IMListKind.SoftSell, id, out var error))
                        {
                            Notify.Success($"已将 {ExcelItemHelper.GetName(id)} 添加到快速探险出售列表");
                        }
                        else
                        {
                            if(EzThrottler.Throttle($"Error_{error}", 1000)) Notify.Error(error);
                        }
                    }
                }
                if(ImGui.GetIO().KeyCtrl)
                {
                    if(!selectedSettings.IMProtectList.Contains(id) && !selectedSettings.IMAutoVendorHard.Contains(id) && !selectedSettings.IMAutoVendorSoft.Contains(id))
                    {
                        if(selectedSettings.AddItemToList(IMListKind.HardSell, id, out var error))
                        {
                            Notify.Success($"已将 {ExcelItemHelper.GetName(id)} 添加到无条件出售列表");
                        }
                        else
                        {
                            if(EzThrottler.Throttle($"Error_{error}", 1000)) Notify.Error(error);
                        }
                    }
                }
                if(!CSFramework.Instance()->WindowInactive && IsKeyPressed(Keys.Tab))
                {
                    if(!selectedSettings.IMProtectList.Contains(id) && !selectedSettings.IMDiscardList.Contains(id))
                    {
                        if(selectedSettings.AddItemToList(IMListKind.Discard, id, out var error))
                        {
                            Notify.Success($"已将 {ExcelItemHelper.GetName(id)} 添加到丢弃列表");
                        }
                        else
                        {
                            if(EzThrottler.Throttle($"Error_{error}", 1000)) Notify.Error(error);
                        }
                    }
                }
                /*if(!CSFramework.Instance()->WindowInactive && IsKeyPressed(Keys.Space))
                {
                    if(!selectedSettings.IMProtectList.Contains(id) && !selectedSettings.IMDesynth.Contains(id))
                    {
                        if(selectedSettings.AddItemToList(IMListKind.Desynth, id, out var error))
                        {
                            Notify.Success($"Added {ExcelItemHelper.GetName(id)} to Desynthesis List");
                        }
                        else
                        {
                            if(EzThrottler.Throttle($"Error_{error}", 1000)) Notify.Error(error);
                        }
                    }
                }*/
                if(ImGui.GetIO().KeyAlt)
                {
                    if(selectedSettings.IMAutoVendorSoft.Remove(id)) Notify.Info($"已将 {ExcelItemHelper.GetName(id)} 从快速探险出售列表移除");
                    if(selectedSettings.IMAutoVendorHard.Remove(id)) Notify.Info($"已将 {ExcelItemHelper.GetName(id)} 从无条件出售列表移除");
                    if(selectedSettings.IMDiscardList.Remove(id)) Notify.Info($"已将 {ExcelItemHelper.GetName(id)} 从丢弃列表移除");
                    if(selectedSettings.IMDesynth.Remove(id)) Notify.Info($"已将 {ExcelItemHelper.GetName(id)} 从分解列表移除");
                }
            }
        });
        DisplayPriority = -10;
    }
}
