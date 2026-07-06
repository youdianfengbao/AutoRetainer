using System;
using System.Collections.Generic;
using System.Text;

namespace AutoRetainer.UI.NeoUI.MultiModeEntries;

public class MultiModeDisableRender : NeoUIEntry
{
    public override string Path => "多角色模式/禁用渲染";

    public override NuiBuilder Builder => new NuiBuilder()
        .Section("禁用渲染")
        .Checkbox("多角色模式时禁用渲染", () => ref C.MultiDisableRender, "在多角色模式期间禁用世界渲染。")
        .Checkbox("仅夜间模式", () => ref C.MultiDisableRenderNightModeOnly)
        .Checkbox("仅窗口未激活时", () => ref C.MultiDisableRenderOnlyInactive);
}
