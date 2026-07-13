using AutoRetainer.Internal;
using AutoRetainerAPI.Configuration;

namespace AutoRetainer.UI.MainWindow.MultiModeTab;

internal static unsafe class MultiModeUI
{
    internal static bool JustRelogged = false;
    private static Dictionary<string, (Vector2 start, Vector2 end)> bars = [];
    private static float StatusTextWidth = 0f;
    internal static void Draw()
    {
        List<OverlayTextData> overlayTexts = [];
        C.OfflineData.RemoveAll(x => C.Blacklist.Any(z => z.CID == x.CID));
        var sortedData = new List<OfflineCharacterData>();
        var shouldExpand = false;
        var doExpand = JustRelogged && !C.NoCurrentCharaOnTop;
        JustRelogged = false;
        if(C.NoCurrentCharaOnTop)
        {
            sortedData = C.OfflineData.ApplyOrder(C.RetainersVisualOrders);
        }
        else
        {
            if(C.OfflineData.TryGetFirst(x => x.CID == Svc.ClientState.LocalContentId, out var cdata))
            {
                sortedData.Add(cdata);
                shouldExpand = true;
            }
            foreach(var x in C.OfflineData.ApplyOrder(C.RetainersVisualOrders))
            {
                if(x.CID != Svc.ClientState.LocalContentId)
                {
                    sortedData.Add(x);
                }
            }
        }
        UIUtils.DrawSearch();

        for(var index = 0; index < sortedData.Count; index++)
        {
            var data = sortedData[index];
            if(data.World.IsNullOrEmpty() || data.ExcludeRetainer) continue;
            var search = Ref<string>.Get("SearchChara");
            if(search != "" && !$"{data.Name}@{data.World}".Contains(search, StringComparison.OrdinalIgnoreCase)) continue;
            ImGui.PushID(data.CID.ToString());
            var rCurPos = ImGui.GetCursorPos();
            var colen = false;
            if(data.Enabled)
            {
                ImGui.PushStyleColor(ImGuiCol.Button, 0xFF097000);
                colen = true;
            }
            if(ImGuiEx.IconButton(Lang.IconMultiMode))
            {
                data.Enabled = !data.Enabled;
            }
            if(colen) ImGui.PopStyleColor();
            ImGuiEx.Tooltip($"为此角色启用多角色模式");
            ImGuiEx.DragDropRepopulate("EnMulti", data.Enabled, ref data.Enabled);
            ImGui.SameLine(0, 3);
            if(ImGuiEx.IconButton(FontAwesomeIcon.DoorOpen))
            {
                if(MultiMode.Relog(data, out var error, RelogReason.ConfigGUI))
                {
                    Notify.Success("正在重新登录...");
                }
                else
                {
                    Notify.Error(error);
                }
            }
            if(ImGui.IsItemClicked(ImGuiMouseButton.Right))
            {
                Copy($"/ays relog {data.Name}@{data.World}");
            }
            ImGuiEx.Tooltip($"左键 - 重新登录到此角色\n右键 - 复制重新登录命令到剪贴板");
            ImGui.SameLine(0, 3);
            if(ImGuiEx.IconButton(FontAwesomeIcon.UserCog))
            {
                ImGui.OpenPopup($"popup{data.CID}");
            }
            if(ImGui.IsItemClicked(ImGuiMouseButton.Right))
            {
                Copy($"{data.Name}@{data.World}");
            }
            ImGuiEx.Tooltip($"配置角色");
            ImGui.SameLine(0, 3);

            if(ImGui.BeginPopup($"popup{data.CID}"))
            {
                CharaConfig.Draw(data, true);
                ImGui.EndPopup();
            }
            data.DrawDCV();
            UIUtils.DrawTeleportIcons(data.CID);
            SharedUI.DrawLockout(data);

            var initCurpos = ImGui.GetCursorPos();
            var lowestRetainer = C.MultiModeRetainerConfiguration.MultiWaitForAll ? data.GetEnabledRetainers().OrderBy(z => z.GetVentureSecondsRemaining()).LastOrDefault() : data.GetEnabledRetainers().OrderBy(z => z.GetVentureSecondsRemaining()).FirstOrDefault();
            if(lowestRetainer != default)
            {
                var prog = Math.Max(0, (3600 - lowestRetainer.GetVentureSecondsRemaining(false)) / 3600f);
                var pcol = prog == 1f ? (C.NoGradient ? 0xbb005000.ToVector4() : GradientColor.Get(0xbb500000.ToVector4(), 0xbb005000.ToVector4())) : 0xbb500000.ToVector4();
                ImGui.PushStyleColor(ImGuiCol.PlotHistogram, pcol);
                ImGui.ProgressBar(prog, new(ImGui.GetContentRegionAvail().X, ImGui.CalcTextSize("A").Y + ImGui.GetStyle().FramePadding.Y * 2), "");
                ImGui.PopStyleColor();
                ImGui.SetCursorPos(initCurpos);
            }
            var colpref = UIUtils.PushColIfPreferredCurrent(data);

            if(shouldExpand && doExpand)
            {
                ImGui.SetNextItemOpen(index == 0);
            }
            if(ImGui.CollapsingHeader(data.GetCutCharaString(StatusTextWidth) + $"###workshop{data.CID}" + $"###chara{data.CID}"))
            {
                SetAsPreferred(data);
                if(colpref)
                {
                    ImGui.PopStyleColor();
                    colpref = false;
                }
                var enabledRetainers = data.GetEnabledRetainers();
                ImGui.PushID(data.CID.ToString());

                var storePos = ImGui.GetCursorPos();
                var retainerData = data.RetainerData;
                foreach(var ret in retainerData)
                {
                    if(bars.TryGetValue($"{data.CID}{ret.Name}", out var v))
                    {
                        if(!ret.HasVenture || ret.Level == 0 || ret.Name.ToString().IsNullOrEmpty()) continue;
                        ImGui.SetCursorPos(v.start - ImGui.GetStyle().CellPadding with { Y = 0 });
                        ImGui.PushStyleColor(ImGuiCol.PlotHistogram, 0xbb500000);
                        ImGui.PushStyleColor(ImGuiCol.FrameBg, 0);
                        ImGui.ProgressBar(1f - Math.Min(1f, ret.GetVentureSecondsRemaining(false) / (60f * 60f)),
                            new(ImGui.GetContentRegionAvail().X, v.end.Y - v.start.Y - ImGui.GetStyle().CellPadding.Y), "");
                        ImGui.PopStyleColor(2);
                    }
                }
                ImGui.SetCursorPos(storePos);
                RetainerTable.Draw(data, retainerData, bars);
                ImGui.Dummy(new(2, 2));
                ImGui.PopID();
            }
            else
            {
                SetAsPreferred(data);
                if(colpref)
                {
                    ImGui.PopStyleColor();
                    colpref = false;
                }
            }
            ImGui.SameLine(0, 0);
            List<(bool, string)> texts = [(data.Ventures < C.UIWarningRetVentureNum, $"探险币: {data.Ventures}"), (data.InventorySpace < C.UIWarningRetSlotNum, $"空间: {data.InventorySpace}")];
            if(C.CharEqualize && MultiMode.Enabled)
            {
                texts.Insert(0, (false, $"计数: {MultiMode.CharaCnt.GetOrDefault(data.CID)}"));
            }
            overlayTexts.Add((new Vector2(ImGui.GetContentRegionMax().X - ImGui.GetStyle().FramePadding.X, rCurPos.Y + ImGui.GetStyle().FramePadding.Y), [.. texts]));
            ImGui.NewLine();
            ImGui.PopID();
        }
        
        StatusTextWidth = 0f;
        UIUtils.DrawOverlayTexts(overlayTexts, ref StatusTextWidth);

        if(C.Verbose && ImGui.CollapsingHeader("调试信息"))
        {
            ImGuiEx.Text($"当前目标角色: {MultiMode.GetCurrentTargetCharacter()}");
            //ImGuiEx.Text($"Yes Already: {YesAlready.IsEnabled()}");
            ImGuiEx.Text($"当前角色是否完成: {MultiMode.IsCurrentCharacterDone()}");
            ImGuiEx.Text($"下次交互时间: {Math.Max(0, MultiMode.NextInteractionAt - Environment.TickCount64)}");
            ImGuiEx.Text($"角色有效性检查: {MultiMode.EnsureCharacterValidity(true)}");
            ImGuiEx.Text($"是否允许交互: {MultiMode.IsInteractionAllowed()}");
            ImGuiEx.Text($"首选角色: {MultiMode.GetPreferredCharacter()}");
            ImGuiEx.Text($"所有雇员是否都有超过15分钟: {MultiMode.IsAllRetainersHaveMoreThan15Mins()}");
            ImGuiEx.Text($"目标角色 ?? 首选角色: {MultiMode.GetCurrentTargetCharacter() ?? MultiMode.GetPreferredCharacter()}");
            //ImGuiEx.Text($"GetAutoAfkOpt: {MultiMode.GetAutoAfkOpt()}");
            //ImGuiEx.Text($"AutoAfkValue: {ConfigModule.Instance()->GetIntValue(145)}");
            ImGuiEx.Text($"最后登录时间: {MultiMode.LastLogin:X16}");
            ImGuiEx.Text($"是否有可用雇员: {MultiMode.AnyRetainersAvailable()}");
            ImGuiEx.Text($"是否有雇员在60秒内完成: {MultiMode.IsAnySelectedRetainerFinishesWithin(60)}");
            ImGuiEx.Text($"是否有雇员在5分钟内完成: {MultiMode.IsAnySelectedRetainerFinishesWithin(5 * 60)}");
            foreach(var data in C.OfflineData)
            {
                ImGuiEx.Text($"角色 {data}\n  所需探险币数量: {data.GetNeededVentureAmount()}");
            }
        }
    }

    internal static void SetAsPreferred(OfflineCharacterData x)
    {
        if(ImGui.IsItemClicked(ImGuiMouseButton.Right))
        {
            if(x.Preferred)
            {
                x.Preferred = false;
            }
            else
            {
                C.OfflineData.Each(x => x.Preferred = false);
                x.Preferred = true;
            }
        }
    }
}
