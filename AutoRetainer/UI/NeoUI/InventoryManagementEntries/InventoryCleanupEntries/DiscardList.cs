using AutoRetainerAPI.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoRetainer.UI.NeoUI.InventoryManagementEntries.InventoryCleanupEntries;
public unsafe sealed class DiscardList : InventoryManagementBase
{
    public override string Name => "物品清理/丢弃列表";
    private InventoryManagementCommon InventoryManagementCommon = new();

    public override int DisplayPriority => -1;

    private DiscardList()
    {
        Builder = InventoryCleanupCommon.CreateCleanupHeaderBuilder()
            .Section(Name)
            .TextWrapped("这些物品将始终被丢弃，无论其来源如何，只要其堆叠数量不超过下方指定的数量。丢弃操作非常频繁，在可能改变背包的每个操作前后都会进行。丢弃始终优先，即使同一物品也存在于出售或分解列表中，也会被丢弃。受保护的物品不会被丢弃。")
            .InputInt(150f, $"最大可丢弃堆叠数量", () => ref InventoryCleanupCommon.SelectedPlan.IMDiscardStackLimit)
            .Widget(() => InventoryManagementCommon.DrawListNew(
                itemId => InventoryCleanupCommon.SelectedPlan.AddItemToList(IMListKind.Discard, itemId, out _),
                itemId => InventoryCleanupCommon.SelectedPlan.IMDiscardList.Remove(itemId),
                InventoryCleanupCommon.SelectedPlan.IMDiscardList,
                (x) =>
                {
                    ImGui.SameLine();
                    ImGui.PushFont(UiBuilder.IconFont);
                    ImGuiEx.CollectionButtonCheckbox(FontAwesomeIcon.Database.ToIconString(), x, InventoryCleanupCommon.SelectedPlan.IMDiscardIgnoreStack);
                    ImGui.PopFont();
                    ImGuiEx.Tooltip($"忽略此物品的堆叠设置");
                }))
            .Separator()
            .Widget(() =>
            {
                InventoryManagementCommon.ImportFromArDiscard(InventoryCleanupCommon.SelectedPlan.IMDiscardList);
            });
    }
}