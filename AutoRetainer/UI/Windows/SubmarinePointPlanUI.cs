using AutoRetainer.Modules.Voyage;
using AutoRetainer.Modules.Voyage.VoyageCalculator;
using AutoRetainerAPI.Configuration;
using ECommons.GameHelpers;
using Lumina.Excel.Sheets;
using Newtonsoft.Json;

namespace AutoRetainer.UI.Windows;

internal unsafe class SubmarinePointPlanUI : Window
{
    internal string SelectedPlanGuid = Guid.Empty.ToString();
    internal string SelectedPlanName => VoyageUtils.GetSubmarinePointPlanByGuid(SelectedPlanGuid).GetPointPlanName();
    internal SubmarinePointPlan SelectedPlan => VoyageUtils.GetSubmarinePointPlanByGuid(SelectedPlanGuid);

    public SubmarinePointPlanUI() : base("潜艇航行路线规划器")
    {
        P.WindowSystem.AddWindow(this);
    }

    internal int GetAmountOfOtherPlanUsers(string guid)
    {
        var i = 0;
        C.OfflineData.Where(x => x.CID != Player.CID).Each(x => i += x.AdditionalSubmarineData.Count(a => a.Value.SelectedPointPlan == guid));
        return i;
    }

    public static readonly string DrawButtonText = "Open Submarine Point Plan Editor";
    public static void DrawButton()
    {
        if(ImGuiEx.IconButtonWithText((FontAwesomeIcon)Lang.IconPlanner[0], DrawButtonText))
        {
            P.SubmarinePointPlanUI.IsOpen = true;
        }
    }

    public override void Draw()
    {
        C.SubmarinePointPlans.RemoveAll(x => x.Delete);
        ImGuiEx.InputWithRightButtonsArea("SUPSelector", () =>
        {
            if(ImGui.BeginCombo("##supsel", SelectedPlanName, ImGuiComboFlags.HeightLarge))
            {
                foreach(var x in C.SubmarinePointPlans)
                {
                    if(ImGui.Selectable(x.GetPointPlanName() + $"##{x.GUID}"))
                    {
                        SelectedPlanGuid = x.GUID;
                    }
                }
                ImGui.EndCombo();
            }
        }, () =>
        {
            if(ImGui.Button("新建计划"))
            {
                var x = new SubmarinePointPlan
                {
                    Name = $""
                };
                C.SubmarinePointPlans.Add(x);
                SelectedPlanGuid = x.GUID;
            }
        });
        ImGui.Separator();
        if(SelectedPlan == null)
        {
            ImGuiEx.Text($"未选择计划或计划不存在");
        }
        else
        {
            if(Data != null)
            {
                var users = GetAmountOfOtherPlanUsers(SelectedPlanGuid);
                var my = Data.AdditionalSubmarineData.Where(x => x.Value.SelectedPointPlan == SelectedPlanGuid);
                if(users == 0)
                {
                    if(!my.Any())
                    {
                        ImGuiEx.TextWrapped($"此计划未被任何潜艇使用。");
                    }
                    else
                    {
                        ImGuiEx.TextWrapped($"此计划被 {my.Select(X => X.Key).Print()} 使用。");
                    }
                }
                else
                {
                    if(!my.Any())
                    {
                        ImGuiEx.TextWrapped($"此计划被您其他角色的 {users} 艘潜艇使用。");
                    }
                    else
                    {
                        ImGuiEx.TextWrapped($"此计划被 {my.Select(X => X.Key).Print()} 以及您其他角色的 {users} 艘潜艇使用。");
                    }
                }
            }
            ImGuiEx.TextV("名称: ");
            ImGui.SameLine();
            ImGuiEx.SetNextItemFullWidth();
            ImGui.InputText($"##planname", ref SelectedPlan.Name, 100);
            ImGuiEx.LineCentered($"planbuttons", () =>
            {
                ImGuiEx.TextV($"将此计划应用到:");
                ImGui.SameLine();
                if(ImGui.Button("所有潜艇"))
                {
                    C.OfflineData.Each(x => x.AdditionalSubmarineData.Each(s => s.Value.SelectedPointPlan = SelectedPlanGuid));
                }
                ImGui.SameLine();
                if(ImGui.Button("当前角色的潜艇"))
                {
                    Data.AdditionalSubmarineData.Each(s => s.Value.SelectedPointPlan = SelectedPlanGuid);
                }
                ImGui.SameLine();
                if(ImGui.Button("取消所有应用"))
                {
                    C.OfflineData.Each(x => x.AdditionalSubmarineData.Where(s => s.Value.SelectedPointPlan == SelectedPlanGuid).Each(s => s.Value.SelectedPointPlan = Guid.Empty.ToString()));
                }
            });
            ImGuiEx.LineCentered($"planbuttons2", () =>
            {
                if(ImGui.Button($"复制计划设置"))
                {
                    Copy(JsonConvert.SerializeObject(SelectedPlan));
                }
                ImGui.SameLine();
                if(ImGui.Button($"粘贴计划设置"))
                {
                    try
                    {
                        var plan = JsonConvert.DeserializeObject<SubmarinePointPlan>(Paste());
                        if(!plan.IsModified())
                        {
                            Notify.Error("无法导入剪贴板内容。请确认是否为正确的计划格式。");
                        }
                        else
                        {
                            SelectedPlan.CopyFrom(plan);
                        }
                    }
                    catch(Exception ex)
                    {
                        DuoLog.Error($"无法导入计划: {ex.Message}");
                        ex.Log();
                    }
                }
                ImGui.SameLine();
                if(ImGuiEx.ButtonCtrl("删除此计划"))
                {
                    SelectedPlan.Delete = true;
                }
            });

            ImGuiEx.EzTableColumns("SubPlan",
            [
                delegate
                {
                    if(ImGui.BeginChild("col1"))
                    {
                        foreach(var x in Svc.Data.GetExcelSheet<SubmarineExploration>())
                        {
                            if(x.Destination.GetText() == "")
                            {
                                if(x.Map.Value.Name.GetText() != "")
                                {
                                    ImGui.Separator();
                                    ImGuiEx.Text($"{x.Map.Value.Name}:");
                                }
                                continue;
                            }
                            var disabled = !SelectedPlan.GetMapId().EqualsAny(0u, x.Map.RowId) || SelectedPlan.Points.Count >= 5 && !SelectedPlan.Points.Contains(x.RowId);
                            if (disabled) ImGui.BeginDisabled();
                            var cont = SelectedPlan.Points.Contains(x.RowId);
                            if (ImGui.Selectable(x.FancyDestination(), cont))
                            {
                                SelectedPlan.Points.Toggle(x.RowId);
                            }
                            if (disabled) ImGui.EndDisabled();
                        }
                    }
                    ImGui.EndChild();
                }, delegate
                {
                    if(ImGui.BeginChild("Col2"))
                    {
                        var map = SelectedPlan.GetMap();
                        if(map != null)
                        {
                            ImGuiEx.Text($"{map.Value.Name}:");
                        }
                        var toRem = -1;
                        for (var i = 0; i < SelectedPlan.Points.Count; i++)
                        {
                            ImGui.PushID(i);
                            if(ImGuiEx.IconButton(FontAwesomeIcon.ArrowUp) && i > 0)
                            {
                                (SelectedPlan.Points[i-1], SelectedPlan.Points[i]) = (SelectedPlan.Points[i], SelectedPlan.Points[i-1]);
                            }
                            ImGui.SameLine();
                            if(ImGuiEx.IconButton(FontAwesomeIcon.ArrowDown) && i < SelectedPlan.Points.Count - 1)
                            {
                                (SelectedPlan.Points[i+1], SelectedPlan.Points[i]) = (SelectedPlan.Points[i], SelectedPlan.Points[i+1]);
                            }
                            ImGui.SameLine();
                            if (ImGuiEx.IconButton(FontAwesomeIcon.Trash))
                            {
                                toRem = i;
                            }
                            ImGui.SameLine();
                            ImGuiEx.Text($"{VoyageUtils.GetSubmarineExploration(SelectedPlan.Points[i])?.FancyDestination()}");
                            ImGui.PopID();
                        }
                        if(toRem > -1)
                        {
                            SelectedPlan.Points.RemoveAt(toRem);
                        }
                    }
                    ImGui.EndChild();
                }
            ]);
        }
    }
}
