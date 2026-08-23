using AutoRetainer.Modules.Voyage;
using AutoRetainer.Modules.Voyage.VoyageCalculator;
using AutoRetainerAPI.Configuration;
using ECommons.GameHelpers;
using ECommons.Throttlers;
using FFXIVClientStructs.FFXIV.Client.Game;
using Lumina.Excel.Sheets;
using Newtonsoft.Json;

namespace AutoRetainer.UI.Windows;

internal unsafe class SubmarineUnlockPlanUI : Window
{
    internal string SelectedPlanGuid = Guid.Empty.ToString();
    internal string SelectedPlanName => VoyageUtils.GetSubmarineUnlockPlanByGuid(SelectedPlanGuid)?.Name ?? "未选择或未知计划";
    internal SubmarineUnlockPlan SelectedPlan => VoyageUtils.GetSubmarineUnlockPlanByGuid(SelectedPlanGuid);

    public SubmarineUnlockPlanUI() : base("潜水艇航行解锁计划")
    {
        P.WindowSystem.AddWindow(this);
    }

    internal Dictionary<uint, bool> RouteUnlockedCache = [];
    internal Dictionary<uint, bool> RouteExploredCache = [];
    internal int NumUnlockedSubs = 0;

    public static readonly string DrawButtonText = "Open Submarine Unlock Plan Editor";
    public static void DrawButton()
    {
        if(ImGuiEx.IconButtonWithText(FontAwesomeIcon.LockOpen, DrawButtonText))
        {
            P.SubmarineUnlockPlanUI.IsOpen = true;
        }
    }

    internal bool IsMapUnlocked(uint map, bool bypassCache = false)
    {
        if(!IsSubDataAvail()) return false;
        var throttle = $"Voyage.MapUnlockedCheck.{map}";
        if(!bypassCache && RouteUnlockedCache.TryGetValue(map, out var val) && !EzThrottler.Check(throttle))
        {
            return val;
        }
        else
        {
            EzThrottler.Throttle(throttle, 2500, true);
            RouteUnlockedCache[map] = HousingManager.IsSubmarineExplorationUnlocked((byte)map);
            return RouteUnlockedCache[map];
        }
    }

    internal bool IsMapExplored(uint map, bool bypassCache = false)
    {
        if(!IsSubDataAvail()) return false;
        var throttle = $"Voyage.MapExploredCheck.{map}";
        if(!bypassCache && RouteExploredCache.TryGetValue(map, out var val) && !EzThrottler.Check(throttle))
        {
            return val;
        }
        else
        {
            EzThrottler.Throttle(throttle, 2500, true);
            RouteExploredCache[map] = HousingManager.IsSubmarineExplorationExplored((byte)map);
            return RouteExploredCache[map];
        }
    }

    internal int? GetNumUnlockedSubs()
    {
        if(!IsSubDataAvail()) return null;
        NumUnlockedSubs = 1 + Unlocks.PointToUnlockPoint.Where(x => x.Value.Sub).Where(x => IsMapExplored(x.Key)).Count();
        return NumUnlockedSubs;
    }

    internal bool IsSubDataAvail()
    {
        if(HousingManager.Instance()->WorkshopTerritory == null) return false;
        if(HousingManager.Instance()->WorkshopTerritory->Submersible.Data.Length == 0) return false;
        if(HousingManager.Instance()->WorkshopTerritory->Submersible.Data[0].Name[0] == 0) return false;
        return true;
    }

    internal int GetAmountOfOtherPlanUsers(string guid)
    {
        var i = 0;
        C.OfflineData.Where(x => x.CID != Player.CID).Each(x => i += x.AdditionalSubmarineData.Count(a => a.Value.SelectedUnlockPlan == guid));
        return i;
    }

    public override void Draw()
    {
        C.SubmarineUnlockPlans.RemoveAll(x => x.Delete);
        ImGuiEx.InputWithRightButtonsArea("SUPSelector", () =>
        {
            if(ImGui.BeginCombo("##supsel", SelectedPlanName, ImGuiComboFlags.HeightLarge))
            {
                foreach(var x in C.SubmarineUnlockPlans)
                {
                    if(ImGui.Selectable(x.Name + $"##{x.GUID}"))
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
                var x = new SubmarineUnlockPlan();
                x.Name = $"计划 {x.GUID}";
                C.SubmarineUnlockPlans.Add(x);
                SelectedPlanGuid = x.GUID;
            }
        });
        ImGui.Separator();
        if(SelectedPlan == null)
        {
            ImGuiEx.Text($"未选择或未知计划");
        }
        else
        {
            if(Data != null)
            {
                var users = GetAmountOfOtherPlanUsers(SelectedPlanGuid);
                var my = Data.AdditionalSubmarineData.Where(x => x.Value.SelectedUnlockPlan == SelectedPlanGuid);
                if(users == 0)
                {
                    if(!my.Any())
                    {
                        ImGuiEx.TextWrapped($"此计划未被任何潜水艇使用。");
                    }
                    else
                    {
                        ImGuiEx.TextWrapped($"此计划被以下潜水艇使用 {my.Select(X => X.Key).Print()}.");
                    }
                }
                else
                {
                    if(!my.Any())
                    {
                        ImGuiEx.TextWrapped($"此计划被您的其他角色的 {users} 艘潜水艇使用。");
                    }
                    else
                    {
                        ImGuiEx.TextWrapped($"此计划被 {my.Select(X => X.Key).Print()} 以及您其他角色的 {users} 艘潜水艇使用。");
                    }
                }
            }
            if(C.DefaultSubmarineUnlockPlan == SelectedPlanGuid)
            {
                ImGuiEx.Text($"此计划已设为默认计划。");
                ImGui.SameLine();
                if(ImGui.SmallButton("重置")) C.DefaultSubmarineUnlockPlan = "";
            }
            else
            {
                if(ImGui.SmallButton("设为默认计划")) C.DefaultSubmarineUnlockPlan = SelectedPlanGuid;
            }
            ImGuiEx.TextV("名称: ");
            ImGui.SameLine();
            ImGuiEx.SetNextItemFullWidth();
            ImGui.InputText($"##planname", ref SelectedPlan.Name, 100);
            ImGuiEx.LineCentered($"planbuttons", () =>
            {
                ImGuiEx.TextV($"应用此计划至:");
                ImGui.SameLine();
                if(ImGui.Button("所有潜水艇"))
                {
                    C.OfflineData.Each(x => x.AdditionalSubmarineData.Each(s => s.Value.SelectedUnlockPlan = SelectedPlanGuid));
                }
                ImGui.SameLine();
                if(ImGui.Button("当前角色的潜水艇"))
                {
                    Data.AdditionalSubmarineData.Each(s => s.Value.SelectedUnlockPlan = SelectedPlanGuid);
                }
                ImGui.SameLine();
                if(ImGui.Button("取消应用"))
                {
                    C.OfflineData.Each(x => x.AdditionalSubmarineData.Where(s => s.Value.SelectedUnlockPlan == SelectedPlanGuid).Each(s => s.Value.SelectedUnlockPlan = Guid.Empty.ToString()));
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
                        var unlockPlan = JsonConvert.DeserializeObject<SubmarineUnlockPlan>(Paste());
                        if(!unlockPlan.IsModified())
                        {
                            Notify.Error("无法导入剪贴板内容。请确认是否为正确的计划格式。");
                        }
                        else
                        {
                            SelectedPlan.CopyFrom(unlockPlan);
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
                ImGui.SameLine();
                if(ImGui.Button($"帮助"))
                {
                    Svc.Chat.Print($"以下是所有可解锁点的列表。当插件需要选择解锁内容时，将从此列表中选择第一个可用的目的地。请注意，您不能简单地指定解锁的终点，需要选择路径上的所有目的地。");
                }
            });
            if(ImGui.BeginChild("Plan"))
            {
                if(!IsSubDataAvail())
                {
                    ImGuiEx.TextWrapped($"请访问潜水艇列表以获取数据。");
                }
                ImGui.Checkbox($"解锁潜水艇槽位。当前槽位: {GetNumUnlockedSubs()?.ToString() ?? "未知"}/4", ref SelectedPlan.UnlockSubs);
                ImGuiEx.TextWrapped($"解锁槽位始终优先于解锁航线。");
                ImGui.Checkbox("在深海站点强制使用单一目的地模式", ref SelectedPlan.EnforceDSSSinglePoint);
                ImGui.Checkbox("强制应用此计划", ref SelectedPlan.EnforcePlan);
                ImGuiEx.HelpMarker("在此地图中选择解锁的任何点将由每一艘符合条件的潜水艇执行，直到所有内容实际解锁");
                if(ImGui.BeginTable("##planTable", 3, ImGuiTableFlags.SizingFixedFit | ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg))
                {
                    ImGui.TableSetupColumn("区域", ImGuiTableColumnFlags.WidthStretch);
                    ImGui.TableSetupColumn("地图");
                    ImGui.TableSetupColumn("解锁方式");
                    ImGui.TableHeadersRow();
                    foreach(var x in Unlocks.PointToUnlockPoint)
                    {
                        if(x.Value.Point < 9000)
                        {
                            ImGui.PushID($"{x.Key}");
                            ImGui.TableNextRow();
                            ImGui.TableNextColumn();
                            var data = Svc.Data.GetExcelSheet<SubmarineExploration>().GetRowOrDefault(x.Key);
                            if(data != null)
                            {
                                try
                                {
                                    var col = IsMapUnlocked(x.Key);
                                    ImGuiEx.CollectionCheckbox($"{data?.FancyDestination()}", x.Key, SelectedPlan.ExcludedRoutes, true);
                                    if(col) ImGui.PushStyleColor(ImGuiCol.Text, ImGuiColors.ParsedGreen);
                                    if(col) ImGui.PopStyleColor();
                                    ImGui.TableNextColumn();
                                    ImGuiEx.TextV($"{data?.Map.ValueNullable?.Name}");
                                    ImGui.TableNextColumn();
                                    var notEnabled = !SelectedPlan.ExcludedRoutes.Contains(x.Key) && SelectedPlan.ExcludedRoutes.Contains(x.Value.Point);
                                    ImGuiEx.TextV(notEnabled ? ImGuiColors.DalamudRed : null, $"{Svc.Data.GetExcelSheet<SubmarineExploration>().GetRowOrDefault(x.Value.Point)?.FancyDestination()}");
                                }
                                catch(Exception e)
                                {
                                    e.Log();
                                }
                            }
                            ImGui.PopID();
                        }
                    }
                    ImGui.EndTable();
                }
                if(ImGui.CollapsingHeader("显示当前点探索顺序"))
                {
                    ImGuiEx.Text(SelectedPlan.GetPrioritizedPointList().Select(x => $"{Svc.Data.GetExcelSheet<SubmarineExploration>().GetRowOrDefault(x.point)?.Destination} ({x.justification})").Join("\n"));
                }
            }
            ImGui.EndChild();
        }
    }
}
