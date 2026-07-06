using ECommons.Configuration;
using ECommons.Reflection;

namespace AutoRetainer.UI.NeoUI.AdvancedEntries;
public class ExpertTab : NeoUIEntry
{
    public override string Path => "高级设置/专家设置";

    public override NuiBuilder Builder { get; init; } = new NuiBuilder()
        .Section("行为设置")
        .EnumComboFullWidth(null, "无可用派遣任务时访问雇员铃的操作:", () => ref C.OpenBellBehaviorNoVentures)
        .EnumComboFullWidth(null, "有可用派遣任务时访问雇员铃的操作:", () => ref C.OpenBellBehaviorWithVentures)
        .EnumComboFullWidth(null, "访问雇员铃后任务完成行为:", () => ref C.TaskCompletedBehaviorAccess)
        .EnumComboFullWidth(null, "手动启用后任务完成行为:", () => ref C.TaskCompletedBehaviorManual)
        .EnumComboFullWidth(null, "插件运行期间任务完成行为:", () => ref C.TaskCompletedBehaviorAuto)
        .TextWrapped(ImGuiColors.DalamudGrey, "多角色模式运行期间，上述3个设置中的\"关闭雇员列表并禁用插件\"选项将被强制启用。")
        .Checkbox("如果5分钟内雇员有派遣任务完成，则停留在雇员菜单", () => ref C.Stay5, "此选项在多角色模式运行期间强制启用。")
        .Checkbox($"关闭雇员列表时自动禁用插件", () => ref C.AutoDisable, "仅当您手动退出菜单时生效。其他情况下由上述设置控制。")
        .Checkbox($"不显示插件状态图标", () => ref C.HideOverlayIcons)
        .Checkbox($"显示多角色模式类型选择器", () => ref C.DisplayMMType)
        .Checkbox($"在部队工房显示远航探索复选框", () => ref C.ShowDeployables)
        .Checkbox("启用应急恢复模块", () => ref C.EnableBailout)
        .InputInt(150f, "AutoRetainer尝试解除卡死前的超时时间(秒)", () => ref C.BailoutTimeout)

        .Section("常规设置")
        .Widget("跳过旅馆登录动画", text =>
        {
            ImGui.SetNextItemWidth(200);
            if(ImGuiEx.EnumCombo(text, ref C.CutsceneSkipMode))
            {
                S.InnCutsceneSkip.RefreshAccordingToConfig();
            }
            ImGuiEx.HelpMarker("跳过过场动画可在服务器端检测到，会增加封号风险", EColor.RedBright, FontAwesomeIcon.ExclamationTriangle.ToIconString());
        })
        .Checkbox($"禁用排序和折叠/展开功能", () => ref C.NoCurrentCharaOnTop)
        .Checkbox($"在插件UI栏显示多角色模式复选框", () => ref C.MultiModeUIBar)
        .SliderIntAsFloat(100f, "雇员菜单延迟(秒)", () => ref C.RetainerMenuDelay.ValidateRange(0, 2000), 0, 2000)
        .Checkbox($"允许派遣计时器显示负值", () => ref C.TimerAllowNegative)
        .Checkbox($"不检查派遣计划错误", () => ref C.NoErrorCheckPlanner2)
        .Checkbox("启用手动重新登录角色后处理", () => ref C.AllowManualPostprocess, "允许在AutoRetainer锁定后处理期间手动调用命令。")
        .Widget("市场冷却状态覆盖", (x) =>
        {
            if(ImGui.Checkbox(x, ref C.MarketCooldownOverlay))
            {
                if(C.MarketCooldownOverlay)
                {
                    P.Memory.OnReceiveMarketPricePacketHook?.Enable();
                }
                else
                {
                    P.Memory.OnReceiveMarketPricePacketHook?.Disable();
                }
            }
        })

        .Section("整合功能")
        .Checkbox($"Artisan 整合功能", () => ref C.ArtisanIntegration, "当派遣任务可收取且附近有雇员铃时，自动启用AutoRetainer并暂停Artisan操作。处理完派遣任务后，Artisan将恢复运行。")

        .Section("服务器时间")
        .Checkbox("使用服务器时间而非本地时间", () => ref C.UseServerTime)

        .Section("实用工具")
        .Widget("清理幽灵雇员", (x) =>
        {
            if(ImGui.Button(x))
            {
                var i = 0;
                foreach(var d in C.OfflineData)
                {
                    i += d.RetainerData.RemoveAll(x => x.Name == "");
                }
                DuoLog.Information($"已清理 {i} 个条目");
            }
        })

        .Section("导入/导出")
        .Widget(() =>
        {
            if(ImGui.Button("导出不含角色数据的配置"))
            {
                var clone = C.JSONClone();
                clone.OfflineData = null;
                clone.AdditionalData = null;
                clone.FCData = null;
                clone.SelectedRetainers = null;
                clone.Blacklist = null;
                clone.AutoLogin = "";
                Copy(EzConfig.DefaultSerializationFactory.Serialize(clone, false));
            }
            if(ImGui.Button("导入并合并角色数据"))
            {
                try
                {
                    var c = EzConfig.DefaultSerializationFactory.Deserialize<Config>(Paste());
                    c.OfflineData = C.OfflineData;
                    c.AdditionalData = C.AdditionalData;
                    c.FCData = C.FCData;
                    c.SelectedRetainers = C.SelectedRetainers;
                    c.Blacklist = C.Blacklist;
                    c.AutoLogin = C.AutoLogin;
                    if(c.GetType().GetFieldPropertyUnions().Any(x => x.GetValue(c) == null)) throw new NullReferenceException();
                    EzConfig.SaveConfiguration(C, $"Backup_{DateTimeOffset.Now.ToUnixTimeMilliseconds()}.json");
                    P.SetConfig(c);
                }
                catch(Exception e)
                {
                    e.LogDuo();
                }
            }
        });
}
