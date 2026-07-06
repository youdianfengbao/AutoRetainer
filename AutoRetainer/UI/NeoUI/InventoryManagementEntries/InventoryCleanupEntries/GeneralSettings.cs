using AutoRetainer.Internal.InventoryManagement;
using ECommons.GameHelpers;
using TerraFX.Interop.Windows;

namespace AutoRetainer.UI.NeoUI.InventoryManagementEntries.InventoryCleanupEntries;
public class GeneralSettings : InventoryManagementBase
{
    public override string Name { get; } = "物品清理/通用设置";

    private GeneralSettings()
    {
        Builder = InventoryCleanupCommon.CreateCleanupHeaderBuilder()
            .Section(Name)
            .Checkbox($"自动开启探险宝箱", () => ref InventoryCleanupCommon.SelectedPlan.IMEnableCofferAutoOpen, "仅多角色模式时启用。注销前将打开所有宝箱，除非背包空间不足。")
            .Indent()
            .InputInt(100f, "单次最多开启数量", () => ref InventoryCleanupCommon.SelectedPlan.MaxCoffersAtOnce)
            .Unindent()
            .Checkbox($"启用出售物品给雇员", () => ref InventoryCleanupCommon.SelectedPlan.IMEnableAutoVendor, "当自动随从派遣雇员进行探险时，将根据物品清理计划出售物品。")
            .Checkbox($"启用出售物品给房屋NPC", () => ref InventoryCleanupCommon.SelectedPlan.IMEnableNpcSell, "当自动随从进入房屋时，将根据物品清理计划出售物品。需要在房屋入口（不是工房入口）附近放置支持物品出售的房屋商人——进入后应立即能与NPC互动。")
            .Indent()
            .Checkbox($"雇员可用时忽略NPC", () => ref InventoryCleanupCommon.SelectedPlan.IMSkipVendorIfRetainer)
            .Widget("立即出售", (x) =>
            {
                if(ImGuiEx.Button(x, Player.Interactable && InventoryCleanupCommon.SelectedPlan.IMEnableNpcSell && NpcSaleManager.GetValidNPC() != null && !IsOccupied() && !P.TaskManager.IsBusy))
                {
                    NpcSaleManager.EnqueueIfItemsPresent(true);
                }
            })
            .Unindent()
            .Checkbox($"自动分解物品", () => ref InventoryCleanupCommon.SelectedPlan.IMEnableItemDesynthesis)
            .Indent()
            .Widget("兵装库: ", t =>
            {
                ImGuiEx.TextV(t);
                ImGui.SameLine();
                ImGuiEx.RadioButtonBool("分解", "跳过", ref InventoryCleanupCommon.SelectedPlan.IMEnableItemDesynthesisFromArmory, true);
            })
            .Unindent()
            .Checkbox($"启用右键菜单集成", () => ref InventoryCleanupCommon.SelectedPlan.IMEnableContextMenu)
            .Checkbox($"允许从兵装库出售/丢弃物品", () => ref InventoryCleanupCommon.SelectedPlan.AllowSellFromArmory)
            .Checkbox("多角色模式时将合格物品存入收藏柜", () => ref InventoryCleanupCommon.SelectedPlan.EnableCabinetAutoDelivery, "未在收藏柜中的物品将被存入。符合条件的物品也将被排除在丢弃、分解、委托给雇员或上交大国防联军之外（仅在多角色模式运行时生效）。这将在多角色模式专家交付之前触发。")
            .Checkbox($"演示模式", () => ref InventoryCleanupCommon.SelectedPlan.IMDry, "不实际出售/丢弃物品，改为在聊天栏输出拟操作内容")
            ;
    }
}
