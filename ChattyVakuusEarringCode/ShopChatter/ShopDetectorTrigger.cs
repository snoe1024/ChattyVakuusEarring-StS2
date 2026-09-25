using System;

namespace ChattyVakuusEarring.ChattyVakuusEarringCode.ShopChatter;

/// <summary>
/// ショップ(商人の部屋)でDetectorが判定を行うタイミング。戦闘中の<c>Chatter.Observer.DetectorTrigger</c>とは
/// 完全に別物(<c>ICombatState</c>に依存しないため)。
/// </summary>
/// <remarks>
/// ショップ画面(<c>NMerchantInventory</c>)が開いている間は、全身像が画面全体に隠れて見えないので、
/// その間は判定しない。「入室した時」「ショップ画面を閉じた時」「閉じた状態のまま放置されている時」の3つだけ。
/// </remarks>
[Flags]
public enum ShopDetectorTrigger
{
    None = 0,

    /// <summary>この部屋(ショップ)に入った直後。まだショップ画面を開いていない状態。</summary>
    RoomEntered = 1 << 0,

    /// <summary>ショップ画面を閉じた(<c>NMerchantInventory.InventoryClosed</c>)瞬間。同じ入室中に何度でも起こりうる。</summary>
    ShopClosed = 1 << 1,

    /// <summary>
    /// 約1秒おきの時間経過。ショップ画面が閉じている間だけ発火する(開いている間は全身像が見えないので判定しない)。
    /// 放置秒数は<c>ShopObserver.IdleSeconds</c>で分かる。
    /// </summary>
    Tick = 1 << 2,
}
