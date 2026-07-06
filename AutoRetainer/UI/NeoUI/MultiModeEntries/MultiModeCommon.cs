namespace AutoRetainer.UI.NeoUI.MultiModeEntries;
public class MultiModeCommon : NeoUIEntry
{
    public override string Path => "多角色模式/通用设置";

    public override NuiBuilder Builder { get; init; } = new NuiBuilder()
        .Section("通用设置")
        .Checkbox($"在登录界面等待", () => ref C.MultiWaitOnLoginScreen, "如果没有角色可进行探险任务，你将保持登出状态直到有角色可用。启用此选项和多角色模式时，标题画面动画将被禁用")
        .Checkbox($"手动登录时禁用多角色模式", () => ref C.MultiDisableOnRelog)
        .Checkbox($"手动登录时不重置首选角色", () => ref C.MultiNoPreferredReset)
        .Checkbox("允许进入共享房屋", () => ref C.SharedHET)
        .Checkbox("即使多角色模式禁用也尝试在登录时进入房屋", () => ref C.HETWhenDisabled)
        .Checkbox("当已在雇员铃旁时不传送或进入房屋", () => ref C.NoTeleportHetWhenNextToBell)

        .Section("游戏启动")
        .Checkbox($"游戏启动时启用多角色模式", () => ref C.MultiAutoStart)
        .Checkbox($"插件启动时启用多角色模式", () => ref C.MultiOnPluginLoad)
        .Indent()
        .SliderInt(150f, "延迟（秒）", () => ref C.MultiModeOnPluginLoadDelay, 0, 20)
        .Unindent()
        .Widget("游戏启动时自动登录", (x) =>
        {
            ImGui.SetNextItemWidth(150f);
            var names = C.OfflineData.Where(s => !s.Name.IsNullOrEmpty()).Select(s => $"{s.Name}@{s.World}");
            var dict = names.ToDictionary(s => s, s => Censor.Character(s));
            dict.Add("", "禁用");
            dict.Add("~", "上次登录的角色");
            ImGuiEx.Combo(x, ref C.AutoLogin, ["", "~", .. names], names: dict);
        })
        .SliderInt(150f, "登录延迟", () => ref C.AutoLoginDelay.ValidateRange(0, 60), 0, 20, "设置适当延迟让插件完全加载后再登录，同时给自己留出取消登录的时间")
        .Checkbox("在插件重新加载后保持多角色模式启用", () => ref C.PreserveMultiModeState)

        .Section("库存警告")
        .InputInt(100f, $"雇员列表：剩余库存槽位警告", () => ref C.UIWarningRetSlotNum.ValidateRange(2, 1000))
        .InputInt(100f, $"雇员列表：剩余探险许可警告", () => ref C.UIWarningRetVentureNum.ValidateRange(2, 1000))
        .InputInt(100f, $"远航探索列表：剩余库存槽位警告", () => ref C.UIWarningDepSlotNum.ValidateRange(2, 1000))
        .InputInt(100f, $"远航探索列表：剩余燃料警告", () => ref C.UIWarningDepTanksNum.ValidateRange(20, 1000))
        .InputInt(100f, $"远航探索列表：剩余修理工具警告", () => ref C.UIWarningDepRepairNum.ValidateRange(5, 1000))

        .Section("传送设置")
        .Widget(() => ImGuiEx.Text("需要安装Lifestream插件"))
        .Widget(() => ImGuiEx.PluginAvailabilityIndicator([new("Lifestream", new Version("2.2.1.1"))]))
        .TextWrapped("必须在Lifestream插件中为每个角色注册房屋才能使此选项生效，或启用简易传送")
        .TextWrapped("可在角色配置菜单中为每个角色自定义这些设置")
        .Widget(() =>
        {
            if(Data != null && Data.GetAreTeleportSettingsOverriden())
            {
                ImGuiEx.TextWrapped(ImGuiColors.DalamudRed, "当前角色已自定义传送选项");
            }
        })
        .Checkbox("启用传送", () => ref C.GlobalTeleportOptions.Enabled)
        .Indent()
        .Checkbox("为雇员传送至...", () => ref C.GlobalTeleportOptions.Retainers)
        .Indent()
        .Checkbox("...个人房屋", () => ref C.GlobalTeleportOptions.RetainersPrivate)
        .Checkbox("...到共享房屋", () => ref C.GlobalTeleportOptions.RetainersShared)
        .Checkbox("...部队房屋", () => ref C.GlobalTeleportOptions.RetainersFC)
        .Checkbox("...公寓", () => ref C.GlobalTeleportOptions.RetainersApartment)
        .TextWrapped("如果以上选项均禁用或失败，将传送至旅馆")
        .Unindent()
        .Checkbox("为航行控制面板传送至部队房屋", () => ref C.GlobalTeleportOptions.Deployables)
        .Checkbox("启用简易传送", () => ref C.AllowSimpleTeleport)
        .Unindent()
        .Widget(() => ImGuiEx.HelpMarker("""
            允许在未向Lifestream注册房屋的情况下传送。传送功能仍需安装Lifestream插件

            警告: 此选项不如在Lifestream中注册房屋可靠。如有可能请避免使用
            """, EColor.RedBright, FontAwesomeIcon.ExclamationTriangle.ToIconString()))

        .Section("应急模块")
        .Checkbox("连接错误时自动关闭游戏并重试登录", () => ref C.ResolveConnectionErrors, "断开连接后，AutoRetainer将尝试重新登录。如果会话已过期，则不会进行登录尝试。")
        .Widget(() => ImGuiEx.PluginAvailabilityIndicator([new("NoKillPlugin")]));
}
