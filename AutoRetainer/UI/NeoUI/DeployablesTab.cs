using AutoRetainer.Internal;
using AutoRetainer.Modules.Voyage;
using AutoRetainer.Modules.Voyage.VoyageCalculator;
using AutoRetainer.UI.Windows;
using AutoRetainerAPI.Configuration;
using Dalamud.Game;
using ECommons;
using ECommons.Interop;
using ECommons.MathHelpers;
using Lumina.Excel.Sheets;
using System.IO;
using System.Windows.Forms;
using OpenFileDialog = ECommons.Interop.OpenFileDialog;
using VesselDescriptor = (ulong CID, string VesselName);

namespace AutoRetainer.UI.NeoUI;
public class DeployablesTab : NeoUIEntry
{
    public override string Path => "远航探索";

    private static int MinLevel = 0;
    private static int MaxLevel = 0;
    private static string Conf = "";
    private static bool InvertConf = false;

    public override NuiBuilder Builder { get; init; }

    public DeployablesTab()
    {
        Builder = new NuiBuilder()
        .Section("常规")
        .Checkbox($"访问远航控制面板时重新派遣舰船", () => ref C.SubsAutoResend2)
        .Checkbox($"重新派遣前完成所有舰船", () => ref C.FinalizeBeforeResend)
        .Checkbox($"在远航探索UI中隐藏飞空艇", () => ref C.HideAirships)

        .Section("计划")
        .Widget(SubmarineUnlockPlanUI.DrawButtonText, x =>
        {
            SubmarineUnlockPlanUI.DrawButton();
        })
        .Widget(SubmarinePointPlanUI.DrawButtonText, x =>
        {
            SubmarinePointPlanUI.DrawButton();
        })

        .Section("警报设置")
        .Checkbox($"启用的舰船数量少于可能数量", () => ref C.AlertNotAllEnabled)
        .Checkbox($"启用的舰船未部署", () => ref C.AlertNotDeployed)
        .Widget("非最优潜水艇配置警报:", (z) =>
        {
            foreach(var x in C.UnoptimalVesselConfigurations)
            {
                ImGuiEx.Text($"等级 {x.MinRank}-{x.MaxRank}, {(x.ConfigurationsInvert ? "NOT " : "")} {x.Configurations.Print()}");
                if(ImGuiEx.HoveredAndClicked("Ctrl+点击删除", default, true))
                {
                    var t = x.GUID;
                    new TickScheduler(() => C.UnoptimalVesselConfigurations.RemoveAll(x => x.GUID == t));
                }
            }

            ImGuiEx.TextV($"等级:");
            ImGui.SameLine();
            ImGuiEx.SetNextItemWidthScaled(60f);
            ImGui.DragInt("##rank1", ref MinLevel, 0.1f);
            ImGui.SameLine();
            ImGuiEx.Text($"-");
            ImGui.SameLine();
            ImGuiEx.SetNextItemWidthScaled(60f);
            ImGui.DragInt("##rank2", ref MaxLevel, 0.1f);
            ImGuiEx.TextV($"配置:");
            ImGui.SameLine();
            ImGui.Checkbox($"NOT", ref InvertConf);
            ImGui.SameLine();
            ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X - 100f.Scale());
            ImGui.InputText($"##conf", ref Conf, 3000);
            ImGui.SameLine();
            if(ImGui.Button("添加"))
            {
                C.UnoptimalVesselConfigurations.Add(new()
                {
                    Configurations = Conf.Split(",", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
                    MinRank = MinLevel,
                    MaxRank = MaxLevel,
                    ConfigurationsInvert = InvertConf
                });
            }
        })
        .Section("批量配置更改")
        .Widget(MassConfigurationChangeWidget)
        .Section("注册、部件和计划自动化")
        .Widget(AutomatedSubPlannerWidget)
        .Section("导出角色和潜水艇列表到CSV")
        .Widget(() =>
        {
            ImGuiEx.FilteringCheckbox("仅导出启用了多角色模式的角色（否则-全部）", out var exportEnabledCharas);
            ImGuiEx.FilteringCheckbox("仅导出已启用的潜水艇（否则-全部）", out var exportEnabledSubs);
            if(ImGuiEx.IconButtonWithText(FontAwesomeIcon.FileExport, "导出"))
            {
                string[] headers = ["名称", "配置 (1)", "配置 (2)", "配置 (3)", "配置 (4)", "等级 (1)", "等级 (2)", "等级 (3)", "等级 (4)", "航线 (1)", "航线 (2)", "航线 (3)", "航线 (4)"];
                List<string[]> data = [];
                foreach(var x in C.OfflineData)
                {
                    if(!x.WorkshopEnabled && exportEnabledCharas) continue;
                    var entry = "".CreateArray((uint)headers.Length);
                    entry[0] = x.NameWithWorld;
                    var list = x.GetVesselData(VoyageType.Submersible);
                    if(list.Count == 0) continue;
                    int i = 0;
                    foreach(var sub in list)
                    {
                        if(exportEnabledSubs && !x.EnabledSubs.Contains(sub.Name)) continue;
                        var a = x.GetAdditionalVesselData(sub.Name, VoyageType.Submersible); ;
                        if(a != null)
                        {
                            entry[i + 1] = a.GetSubmarineBuild().Trim();
                            entry[i + 5] = $"{a.Level}.{(int)(a.CurrentExp * 100f / a.NextLevelExp)}";
                            List<string> points = [];
                            foreach(var s in a.Points)
                            {
                                if(s != 0)
                                {
                                    var d = Svc.Data.GetExcelSheet<SubmarineExploration>(ClientLanguage.Japanese).GetRowOrDefault(s);
                                    if(d != null && d.Value.Location.ToString().Length > 0)
                                    {
                                        points.Add(d.Value.Location.ToString());
                                    }
                                }
                            }
                            entry[i + 9] = $"{points.Join("").Trim()}";
                            i++;
                            if(i > 3) break;
                        }
                    }
                    data.Add(entry);
                }
                OpenFileDialog.SelectFile(x =>
                {
                    var name = x.file;
                    if(!name.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
                    {
                        name = $"{name}.csv";
                    }
                    Utils.WriteCsv(name, headers, data);
                }, title: "另存为...", fileTypes: [("逗号分隔值文件", ["csv"])], save:true);
            }
        });
    }

    private HashSet<VesselDescriptor> SelectedVessels = [];
    private int MassMinLevel = 0;
    private int MassMaxLevel = 120;
    private VesselBehavior MassBehavior = VesselBehavior.Finalize;
    private UnlockMode MassUnlockMode = UnlockMode.WhileLevelling;
    private SubmarineUnlockPlan SelectedUnlockPlan;
    private SubmarinePointPlan SelectedPointPlan;

    private void MassConfigurationChangeWidget()
    {
        ImGuiEx.Text($"选择潜水艇:");
        ImGuiEx.SetNextItemFullWidth();
        if(ImGui.BeginCombo($"##sel", $"已选择 {SelectedVessels.Count}", ImGuiComboFlags.HeightLarge))
        {
            ref var search = ref Ref<string>.Get("Search");
            ImGui.InputTextWithHint("##searchSubs", "搜索角色", ref search, 100);
            foreach(var x in C.OfflineData)
            {
                if(x.ExcludeWorkshop) continue;
                if(search.Length > 0 && !$"{x.Name}@{x.World}".Contains(search, StringComparison.OrdinalIgnoreCase)) continue;
                if(x.OfflineSubmarineData.Count > 0)
                {
                    ImGui.PushID(x.CID.ToString());
                    ImGuiEx.CollectionCheckbox(Censor.Character(x.Name, x.World), x.OfflineSubmarineData.Select(v => (x.CID, v.Name)), SelectedVessels);
                    ImGui.Indent();
                    foreach(var v in x.OfflineSubmarineData)
                    {
                        ImGuiEx.CollectionCheckbox($"{v.Name}", (x.CID, v.Name), SelectedVessels);
                    }
                    ImGui.Unindent();
                    ImGui.PopID();
                }
            }
            ImGui.EndCombo();
        }
        if(ImGuiEx.IconButtonWithText((FontAwesomeIcon)'\uf057', "全部取消"))
        {
            SelectedVessels.Clear();
        }
        ImGui.SameLine();
        if(ImGuiEx.IconButtonWithText((FontAwesomeIcon)'\uf055', "全选"))
        {
            SelectedVessels.Clear();
            foreach(var x in C.OfflineData) foreach(var v in x.OfflineSubmarineData) SelectedVessels.Add((x.CID, v.Name));
        }
        ImGui.Separator();
        ImGuiEx.TextV("按等级:");
        ImGui.SameLine();
        ImGui.SetNextItemWidth(100f);
        ImGui.DragInt("##minlevel", ref MassMinLevel, 0.1f);
        ImGui.SameLine();
        ImGui.SetNextItemWidth(100f);
        ImGui.DragInt("##maxlevel", ref MassMaxLevel, 0.1f);
        if(ImGuiEx.IconButtonWithText(FontAwesomeIcon.Plus, "添加等级范围内的舰船"))
        {
            foreach(var x in C.OfflineData)
            {
                foreach(var v in x.OfflineSubmarineData)
                {
                    var adata = x.GetAdditionalVesselData(v.Name, VoyageType.Submersible);
                    if(adata.Level.InRange(MassMinLevel, MassMaxLevel, true))
                    {
                        SelectedVessels.Add((x.CID, v.Name));
                    }
                }
            }
        }
        ImGui.Separator();
        ImGuiEx.Text("操作:");

        ImGui.Separator();
        ImGui.SetNextItemWidth(150f);
        ImGuiEx.EnumCombo("##behavior", ref MassBehavior);
        ImGui.SameLine();
        if(ImGuiEx.IconButtonWithText((FontAwesomeIcon)'\uf018', "设置行为"))
        {
            var num = 0;
            foreach(var x in SelectedVessels)
            {
                var odata = C.OfflineData.FirstOrDefault(z => z.CID == x.CID);
                if(odata != null)
                {
                    var vdata = odata.GetOfflineVesselData(x.VesselName, VoyageType.Submersible);
                    var adata = odata.GetAdditionalVesselData(x.VesselName, VoyageType.Submersible);
                    adata.VesselBehavior = MassBehavior;
                    num++;
                }
            }
            Notify.Success($"已影响 {num} 艘潜水艇");
        }

        ImGui.Separator();
        ImGui.SetNextItemWidth(150f);
        ImGuiEx.EnumCombo("##unlockmode", ref MassUnlockMode, Lang.UnlockModeNames);
        ImGui.SameLine();
        if(ImGuiEx.IconButtonWithText((FontAwesomeIcon)'\uf09c', "设置解锁模式"))
        {
            var num = 0;
            foreach(var x in SelectedVessels)
            {
                var odata = C.OfflineData.FirstOrDefault(z => z.CID == x.CID);
                if(odata != null)
                {
                    var vdata = odata.GetOfflineVesselData(x.VesselName, VoyageType.Submersible);
                    var adata = odata.GetAdditionalVesselData(x.VesselName, VoyageType.Submersible);
                    adata.UnlockMode = MassUnlockMode;
                    num++;
                }
            }
            Notify.Success($"已影响 {num} 艘潜水艇");
        }

        ImGui.Separator();

        ImGui.SetNextItemWidth(150f);
        if(ImGui.BeginCombo("##uplan", "解锁计划: " + (SelectedUnlockPlan?.Name ?? "未选择", ImGuiComboFlags.HeightLarge)))
        {
            foreach(var plan in C.SubmarineUnlockPlans)
            {
                if(ImGui.Selectable($"{plan.Name}##{plan.GUID}"))
                {
                    SelectedUnlockPlan = plan;
                }
            }
            ImGui.EndCombo();
        }
        ImGui.SameLine();
        if(ImGuiEx.IconButtonWithText((FontAwesomeIcon)'\uf3c1', "设置解锁计划", SelectedUnlockPlan != null))
        {
            var num = 0;
            foreach(var x in SelectedVessels)
            {
                var odata = C.OfflineData.FirstOrDefault(z => z.CID == x.CID);
                if(odata != null)
                {
                    var vdata = odata.GetOfflineVesselData(x.VesselName, VoyageType.Submersible);
                    var adata = odata.GetAdditionalVesselData(x.VesselName, VoyageType.Submersible);
                    adata.SelectedUnlockPlan = SelectedUnlockPlan.GUID.ToString();
                    num++;
                }
            }
            Notify.Success($"已影响 {num} 艘潜水艇");
        }
        ImGui.Separator();

        ImGui.SetNextItemWidth(150f);
        if(ImGui.BeginCombo("##uplan2", "点位计划: " + (VoyageUtils.GetPointPlanName(SelectedPointPlan) ?? "未选择"), ImGuiComboFlags.HeightLarge))
        {
            foreach(var plan in C.SubmarinePointPlans)
            {
                if(ImGui.Selectable($"{VoyageUtils.GetPointPlanName(plan)}##{plan.GUID}"))
                {
                    SelectedPointPlan = plan;
                }
            }
            ImGui.EndCombo();
        }
        ImGui.SameLine();
        if(ImGuiEx.IconButtonWithText((FontAwesomeIcon)'\uf55b', "设置点位计划", SelectedPointPlan != null))
        {
            var num = 0;
            foreach(var x in SelectedVessels)
            {
                var odata = C.OfflineData.FirstOrDefault(z => z.CID == x.CID);
                if(odata != null)
                {
                    var vdata = odata.GetOfflineVesselData(x.VesselName, VoyageType.Submersible);
                    var adata = odata.GetAdditionalVesselData(x.VesselName, VoyageType.Submersible);
                    adata.SelectedPointPlan = SelectedPointPlan.GUID.ToString();
                    num++;
                }
            }
            Notify.Success($"已影响 {num} 艘潜水艇");
        }

        ImGui.Separator();

        if(ImGuiEx.IconButtonWithText(FontAwesomeIcon.Check, "启用选中的潜水艇"))
        {
            var num = 0;
            foreach(var x in SelectedVessels)
            {
                var odata = C.OfflineData.FirstOrDefault(z => z.CID == x.CID);
                if(odata != null)
                {
                    if(odata.EnabledSubs.Add(x.VesselName))
                    {
                        num++;
                    }
                }
            }
            Notify.Success($"已影响 {num} 艘潜水艇");
        }

        ImGui.Separator();

        if(ImGuiEx.IconButtonWithText(FontAwesomeIcon.Times, "禁用选中的潜水艇"))
        {
            var num = 0;
            foreach(var x in SelectedVessels)
            {
                var odata = C.OfflineData.FirstOrDefault(z => z.CID == x.CID);
                if(odata != null)
                {
                    if(odata.EnabledSubs.Remove(x.VesselName))
                    {
                        num++;
                    }
                }
            }
            Notify.Success($"已影响 {num} 艘潜水艇");
        }

        ImGui.Separator();

        if(ImGuiEx.IconButtonWithText(FontAwesomeIcon.CheckCircle, "为选中潜水艇的所有者启用远航探索多角色模式"))
        {
            var num = 0;
            foreach(var x in SelectedVessels)
            {
                var odata = C.OfflineData.FirstOrDefault(z => z.CID == x.CID);
                if(odata != null && !odata.WorkshopEnabled)
                {
                    odata.WorkshopEnabled = true;
                    num++;
                }
            }
            Notify.Success($"已影响 {num} 个角色");
        }

        ImGui.Separator();

        if(ImGuiEx.IconButtonWithText(FontAwesomeIcon.TimesCircle, "为选中潜水艇的所有者禁用远航探索多角色模式"))
        {
            var num = 0;
            foreach(var x in SelectedVessels)
            {
                var odata = C.OfflineData.FirstOrDefault(z => z.CID == x.CID);
                if(odata != null && odata.WorkshopEnabled)
                {
                    odata.WorkshopEnabled = false;
                    num++;
                }
            }
            Notify.Success($"已影响 {num} 个角色");
        }
    }

    private void AutomatedSubPlannerWidget()
    {
        ImGui.Checkbox("启用自动潜水艇注册", ref C.EnableAutomaticSubRegistration);
        ImGui.Checkbox("启用自动部件和计划更改", ref C.EnableAutomaticComponentsAndPlanChange);
        ImGuiEx.Text("等级范围:");
        for(var index = C.LevelAndPartsData.Count - 1; index >= 0; index--)
        {
            var entry = C.LevelAndPartsData[index];
            if(ImGui.CollapsingHeader($"{entry.GetPlanBuild()}: {entry.MinLevel} - {entry.MaxLevel} ###{entry.GUID}"))
            {
                ImGui.Separator();
                ImGui.Text("等级范围:");
                ImGui.SameLine();
                ImGuiEx.SetNextItemWidthScaled(60f);
                ImGui.PushID("##minlvl");
                ImGui.DragInt($"##minlvl{entry.GUID}", ref entry.MinLevel, 0.1f);
                ImGui.PopID();
                ImGui.SameLine();
                ImGuiEx.Text($"-");
                ImGuiEx.SetNextItemWidthScaled(60f);
                ImGui.SameLine();
                ImGui.PushID("##maxlvl");
                ImGui.DragInt($"##maxlvl{entry.GUID}", ref entry.MaxLevel, 0.1f);
                ImGui.PopID();

                ImGui.Text("船体:");
                ImGui.SameLine(60f);
                ImGui.SetNextItemWidth(100f);
                ImGuiEx.EnumCombo($"##hull{entry.GUID}", ref entry.Part1);

                ImGui.Text("船尾:");
                ImGui.SameLine(60f);
                ImGui.SetNextItemWidth(100f);
                ImGuiEx.EnumCombo($"##stern{entry.GUID}", ref entry.Part2);

                ImGui.Text("船首:");
                ImGui.SameLine(60f);
                ImGui.SetNextItemWidth(100f);
                ImGuiEx.EnumCombo($"##bow{entry.GUID}", ref entry.Part3);

                ImGui.Text("舰桥:");
                ImGui.SameLine(60f);
                ImGui.SetNextItemWidth(100f);
                ImGuiEx.EnumCombo($"##bridge{entry.GUID}", ref entry.Part4);

                ImGui.Text("行为:");
                ImGui.SameLine(60f);
                ImGui.SetNextItemWidth(150f);
                ImGuiEx.EnumCombo($"##behavior{entry.GUID}", ref entry.VesselBehavior);
                ImGui.Text("计划:");
                ImGui.SameLine(60f);
                if(entry.VesselBehavior == VesselBehavior.Unlock)
                {
                    ImGui.SetNextItemWidth(150f);
                    if(ImGui.BeginCombo($"##unlockplan{entry.GUID}", C.SubmarineUnlockPlans.Any(x => x.GUID == entry.SelectedUnlockPlan)
                                                                              ? C.SubmarineUnlockPlans.First(x => x.GUID == entry.SelectedUnlockPlan)
                                                                                 .Name
                                                                              : "未选择", ImGuiComboFlags.HeightLarge))
                    {
                        foreach(var plan in C.SubmarineUnlockPlans)
                        {
                            if(ImGui.Selectable($"{plan.Name}##{entry.GUID}"))
                            {
                                entry.SelectedUnlockPlan = plan.GUID;
                            }
                        }

                        ImGui.EndCombo();
                    }

                    ImGui.Text("模式:");
                    ImGui.SameLine(60f);
                    ImGui.SetNextItemWidth(150f);
                    ImGuiEx.EnumCombo($"##unlockmode{entry.GUID}", ref entry.UnlockMode);
                }
                else if(entry.VesselBehavior == VesselBehavior.Use_plan)
                {
                    ImGui.SetNextItemWidth(150f);
                    if(ImGui.BeginCombo($"##pointplan{entry.GUID}", C.SubmarinePointPlans.Any(x => x.GUID == entry.SelectedPointPlan)
                                                                             ? C.SubmarinePointPlans.First(x => x.GUID == entry.SelectedPointPlan).GetPointPlanName()
                                                                             : "未选择", ImGuiComboFlags.HeightLarge))
                    {
                        foreach(var plan in C.SubmarinePointPlans)
                        {
                            if(ImGui.Selectable($"{plan.GetPointPlanName()}##{entry.GUID}"))
                            {
                                entry.SelectedPointPlan = plan.GUID;
                            }
                        }

                        ImGui.EndCombo();
                    }
                }

                ImGui.Separator();
                ImGui.Checkbox($"第一艘潜水艇使用不同配置###firstSubDifferent{entry.GUID}", ref entry.FirstSubDifferent);
                if(entry.FirstSubDifferent)
                {
                    ImGui.Text("第一艘行为:");
                    ImGui.SameLine(150f);
                    ImGui.SetNextItemWidth(150f);
                    ImGuiEx.EnumCombo($"##firstSubBehavior{entry.GUID}", ref entry.FirstSubVesselBehavior);
                    ImGui.Text("第一艘计划:");
                    ImGui.SameLine(150f);
                    if(entry.FirstSubVesselBehavior == VesselBehavior.Unlock)
                    {
                        ImGui.SetNextItemWidth(150f);
                        if(ImGui.BeginCombo($"##firstSubUnlockplan{entry.GUID}", C.SubmarineUnlockPlans.Any(x => x.GUID == entry.FirstSubSelectedUnlockPlan)
                                                     ? C.SubmarineUnlockPlans.First(x => x.GUID == entry.FirstSubSelectedUnlockPlan)
                                                        .Name
                                                     : "未选择", ImGuiComboFlags.HeightLarge))
                        {
                            foreach(var plan in C.SubmarineUnlockPlans)
                            {
                                if(ImGui.Selectable($"{plan.Name}##firstSub{entry.GUID}"))
                                {
                                    entry.FirstSubSelectedUnlockPlan = plan.GUID;
                                }
                            }

                            ImGui.EndCombo();
                        }

                        ImGui.Text("第一艘模式:");
                        ImGui.SameLine(150f);
                        ImGui.SetNextItemWidth(150f);
                        ImGuiEx.EnumCombo($"##firstSubUnlockmode{entry.GUID}", ref entry.FirstSubUnlockMode);
                    }
                    else if(entry.FirstSubVesselBehavior == VesselBehavior.Use_plan)
                    {
                        ImGui.SetNextItemWidth(150f);
                        if(ImGui.BeginCombo($"##firstSubPointplan{entry.GUID}", C.SubmarinePointPlans.Any(x => x.GUID == entry.FirstSubSelectedPointPlan)
                                                     ? C.SubmarinePointPlans.First(x => x.GUID == entry.FirstSubSelectedPointPlan).GetPointPlanName()
                                                     : "未选择", ImGuiComboFlags.HeightLarge))
                        {
                            foreach(var plan in C.SubmarinePointPlans)
                            {
                                if(ImGui.Selectable($"{plan.GetPointPlanName()}##firstSub{entry.GUID}"))
                                {
                                    entry.FirstSubSelectedPointPlan = plan.GUID;
                                }
                            }

                            ImGui.EndCombo();
                        }
                    }
                }

                ImGui.NewLine();
                if(ImGui.Button($"删除##{entry.GUID}"))
                {
                    C.LevelAndPartsData.RemoveAt(index);
                }
            }
        }

        ImGui.Separator();
        if(ImGui.Button("添加"))
        {
            C.LevelAndPartsData.Insert(0, new());
        }
    }
}
