using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoRetainer.UI.NeoUI.InventoryManagementEntries.GCDeliveryEntries;
public sealed unsafe class GeneralSettings : InventoryManagementBase
{
    public override string Name { get; } = "军票交纳/通用设置";

    public override NuiBuilder Builder => new NuiBuilder()
        .Section("通用设置")
        .Checkbox("启用精英交纳续行", () => ref C.AutoGCContinuation)
        .TextWrapped($"""
            启用精英交纳续行后：
            - 插件将自动消耗可用军票，从配置的兑换列表中购买物品。
            - 如果兑换列表为空，则仅购买探险币。
            - 请确保在"角色设置"部分中"交纳模式"未设为"禁用"

            军票消耗完毕后：
            - 精英交纳将自动恢复。
            - 此过程将重复，直到没有符合条件的物品可交纳或军票用尽。
            """)

        .Section("多角色精英交纳")
        .TextWrapped($"""
        启用后：
        - 启用传送的角色将在多角色模式下自动进行精英交纳和按兑换计划购买物品（如果军衔足够）。
        """)
        .Checkbox("启用多角色精英交纳", () => ref C.FullAutoGCDelivery)
        .Checkbox("仅在未锁定时", () => ref C.FullAutoGCDeliveryOnlyWsUnlocked)
        .InputInt(150f, "背包剩余格子数小于等于时触发交纳", () => ref C.FullAutoGCDeliveryInventory, "仅计算主背包，不计算兵装库")
        .Checkbox("探险币耗尽时触发", () => ref C.FullAutoGCDeliveryDeliverOnVentureExhaust, "这可能导致每次登录时都前往军队兑换。请确保已设置购买足够探险币的计划。")
        .Indent()
        .InputInt(150f, "探险币剩余数量小于等于时触发交纳", () => ref C.FullAutoGCDeliveryDeliverOnVentureLessThan)
        .Unindent()
        .Checkbox("尽可能使用定额军票优待", () => ref C.FullAutoGCDeliveryUseBuffItem)
        .Checkbox("尽可能使用部队军票效果", () => ref C.FullAutoGCDeliveryUseBuffFCAction)
        .Checkbox("交纳后传送回房屋/旅馆", () => ref C.TeleportAfterGCExchange)
        .Indent()
        .Checkbox("仅在多角色模式激活时", () => ref C.TeleportAfterGCExchangeMulti)
        .Unindent()
        ;
}