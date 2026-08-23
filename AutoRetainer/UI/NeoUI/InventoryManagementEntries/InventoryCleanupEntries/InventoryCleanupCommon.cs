using AutoRetainerAPI.Configuration;
using ECommons.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoRetainer.UI.NeoUI.InventoryManagementEntries.InventoryCleanupEntries;
public static unsafe class InventoryCleanupCommon
{
    public static Guid SelectedPlanGuid = Guid.Empty;

    public static InventoryManagementSettings SelectedPlan
    {
        get
        {
            if(SelectedPlanGuid == Guid.Empty)
            {
                return C.DefaultIMSettings;
            }
            else
            {
                var planIndex = C.AdditionalIMSettings.IndexOf(x => x.GUID == SelectedPlanGuid);
                if(planIndex == -1)
                {
                    SelectedPlanGuid = Guid.Empty;
                    return C.DefaultIMSettings;
                }
                else
                {
                    return C.AdditionalIMSettings[planIndex];
                }
            }
        }
    }

    public static NuiBuilder CreateCleanupHeaderBuilder()
    {
        return new NuiBuilder().Section("物品清理计划选择").Widget(DrawPlanSelector);
    }

    public static void DrawPlanSelector()
    {
        var selectedPlan = C.AdditionalIMSettings.FirstOrDefault(x => x.GUID == SelectedPlanGuid);
        ImGuiEx.InputWithRightButtonsArea(() =>
        {
            if(ImGui.BeginCombo("##selimplan", selectedPlan?.DisplayName ?? "默认计划"))
            {
                if(ImGui.Selectable("默认计划", selectedPlan == null)) SelectedPlanGuid = Guid.Empty;
                ImGui.Separator();
                foreach(var x in C.AdditionalIMSettings)
                {
                    ImGui.PushID(x.ID);
                    if(ImGui.Selectable(x.DisplayName)) SelectedPlanGuid = x.GUID;
                    ImGui.PopID();
                }
                ImGui.EndCombo();
            }
        }, () =>
        {
            if(ImGuiEx.IconButton(FontAwesomeIcon.Plus))
            {
                var newPlan = new InventoryManagementSettings()
                {
                    AllowSellFromArmory = C.DefaultIMSettings.AllowSellFromArmory,
                    IMEnableContextMenu = C.DefaultIMSettings.IMEnableContextMenu,
                    IMEnableCofferAutoOpen = C.DefaultIMSettings.IMEnableCofferAutoOpen,
                    IMSkipVendorIfRetainer = C.DefaultIMSettings.IMSkipVendorIfRetainer,
                    IMEnableAutoVendor = C.DefaultIMSettings.IMEnableAutoVendor,
                    IMEnableNpcSell = C.DefaultIMSettings.IMEnableNpcSell,
                };
                C.AdditionalIMSettings.Add(newPlan);
                SelectedPlanGuid = newPlan.GUID;
            }
            ImGuiEx.Tooltip("新增计划");
            ImGui.SameLine(0, 1);
            if(ImGuiEx.IconButton(FontAwesomeIcon.Copy))
            {
                var clone = (selectedPlan ?? C.DefaultIMSettings).DSFClone();
                clone.GUID = Guid.Empty;
                Copy(EzConfig.DefaultSerializationFactory.Serialize(clone));
            }
            ImGuiEx.Tooltip("复制");
            ImGui.SameLine(0, 1);
            if(ImGuiEx.IconButton(FontAwesomeIcon.Paste))
            {
                try
                {
                    var newPlan = EzConfig.DefaultSerializationFactory.Deserialize<InventoryManagementSettings>(Paste()) ?? throw new NullReferenceException();
                    newPlan.GUID.Regenerate();
                    C.AdditionalIMSettings.Add(newPlan);
                    SelectedPlanGuid = newPlan.GUID;
                }
                catch(Exception e)
                {
                    e.Log();
                    Notify.Error($"导入失败：剪贴板内容不是有效的库存管理计划格式（{e.Message}）");
                }
            }
            ImGuiEx.Tooltip("粘贴");
            if(selectedPlan != null)
            {
                ImGui.SameLine(0, 1);
                if(ImGuiEx.IconButton(FontAwesomeIcon.ArrowsUpToLine, enabled: ImGuiEx.Ctrl && selectedPlan != null))
                {
                    C.DefaultIMSettings = selectedPlan.DSFClone();
                    C.DefaultIMSettings.GUID.Regenerate();
                    C.DefaultIMSettings.Name = "";
                    new TickScheduler(() => C.AdditionalIMSettings.Remove(selectedPlan));
                }
                ImGuiEx.Tooltip("将此计划设为默认。当前默认计划将被覆盖。按住CTRL点击。");
                ImGui.SameLine(0, 1);
                if(ImGuiEx.IconButton(FontAwesomeIcon.Trash, enabled: ImGuiEx.Ctrl && selectedPlan != null))
                {
                    new TickScheduler(() => C.AdditionalIMSettings.Remove(selectedPlan));
                }
                ImGuiEx.Tooltip("删除此计划。按住CTRL点击。");
            }
        });
        if(selectedPlan != null)
        {
            ImGuiEx.SetNextItemFullWidth();
            ImGui.InputTextWithHint("##name", "输入计划名称", ref selectedPlan.Name, 100);

            if(Data != null)
            {
                if(Data.InventoryCleanupPlan == SelectedPlanGuid)
                {
                    ImGuiEx.Text(ImGuiColors.ParsedGreen, UiBuilder.IconFont, FontAwesomeIcon.Check.ToIconString());
                    ImGui.SameLine();
                    ImGuiEx.Text(ImGuiColors.ParsedGreen, $"当前角色正在使用");
                    ImGui.SameLine();
                    if(ImGui.SmallButton("取消分配"))
                    {
                        Data.InventoryCleanupPlan = Guid.Empty;
                    }
                }
                else
                {
                    ImGuiEx.Text(ImGuiColors.DalamudOrange, UiBuilder.IconFont, FontAwesomeIcon.ExclamationTriangle.ToIconString());
                    ImGui.SameLine();
                    ImGuiEx.Text(ImGuiColors.DalamudOrange, $"当前角色未使用");
                    ImGui.SameLine();
                    if(ImGui.SmallButton("分配"))
                    {
                        Data.InventoryCleanupPlan = selectedPlan.GUID;
                    }
                }
                ImGui.SameLine();
            }

            var charas = C.OfflineData.Where(x => x.ExchangePlan == selectedPlan.GUID).ToArray();
            if(charas.Length > 0)
            {
                ImGuiEx.Text($"共 {charas.Length} 个角色使用");
                ImGuiEx.Tooltip($"{charas.Select(x => x.NameWithWorldCensored)}");
            }
            else
            {
                ImGuiEx.Text($"无任何角色使用");
            }

            ImGuiEx.Text("将此计划的列表与默认计划合并：");
            ImGui.Indent();
            ImGui.Checkbox("合并快速探险出售列表", ref selectedPlan.AdditionModeSoftSellList);
            ImGuiEx.HelpMarker("在此计划和默认计划中都包含的通过快速探险获得的物品将被出售。");
            ImGui.Checkbox("合并无条件出售列表", ref selectedPlan.AdditionModeHardSellList);
            ImGuiEx.HelpMarker("在此计划和默认计划中都包含的物品将被出售。如果同时包含在默认计划和当前计划中，将遵循当前计划的堆叠数量绕过选项。当前计划的\"最大可出售堆叠数量\"选项将覆盖默认计划的选项。");
            ImGui.Checkbox("合并丢弃列表", ref selectedPlan.AdditionModeDiscardList);
            ImGuiEx.HelpMarker("在此计划和默认计划中都包含的物品将被丢弃。如果同时包含在默认计划和当前计划中，将遵循当前计划的堆叠数量绕过选项。当前计划的\"最大可丢弃堆叠数量\"选项将覆盖默认计划的选项。");
            ImGui.Checkbox("合并保护列表", ref selectedPlan.AdditionModeProtectList);
            ImGuiEx.HelpMarker("在此计划和默认计划中都包含的物品将不会被自动出售或上交大国防联军，即使包含在任何列表中。");
            ImGui.Unindent();
        }
    }
}