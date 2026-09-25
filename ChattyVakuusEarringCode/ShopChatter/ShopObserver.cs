using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Merchant;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace ChattyVakuusEarring.ChattyVakuusEarringCode.ShopChatter;

/// <summary>
/// ショップ(商人の部屋)でのプレイヤーの状況を観察する窓口。<c>Chatter.Observer.PlayObserver</c>のショップ版。
/// </summary>
/// <remarks>
/// 戦闘用の<c>PlayObserver</c>とは違い<c>ICombatState</c>を持たない、ごく薄い読み取り専用の層。
/// 判定に必要な情報が足りなくなったら、ここにプロパティ/メソッドを足す(方針は<c>PlayObserver</c>と同じ)。
/// 1つのObserverは1回のショップ入室に対応し、<see cref="ShopChatterSession"/>が生成する。
/// </remarks>
public sealed class ShopObserver
{
    public ShopObserver(Player owner)
    {
        Owner = owner;
    }

    /// <summary>囁きのイヤリングの持ち主(常にこの端末のローカルプレイヤー)。</summary>
    public Player Owner { get; }

    public Creature OwnerCreature => Owner.Creature;

    /// <summary>
    /// ショップ画面が閉じている間、何も操作せずにいる秒数(入室・ショップを閉じた瞬間に0へ戻る)。
    /// <see cref="ShopDetectorTrigger.Tick"/>の間だけ加算される。
    /// </summary>
    public double IdleSeconds { get; private set; }

    internal void AddIdleTime(double seconds) => IdleSeconds += seconds;

    internal void ResetIdle() => IdleSeconds = 0;

    public bool HasRelic<T>() where T : RelicModel => Owner.GetRelic<T>() != null;

    public T? GetRelic<T>() where T : RelicModel => Owner.GetRelic<T>();

    /// <summary>入室した瞬間の所持ゴールド。ショップを閉じた時、いくら使ったかの判定に使う。</summary>
    public int GoldAtEntry { get; internal set; }

    public int Gold => Owner.Gold;

    /// <summary>
    /// 現在のショップの中身(カード・レリック・ポーション・カード除去)。<c>NMerchantRoom.Room.GetLocalInventory()</c>
    /// を毎回引く(ショップの外や取得できない場合はnull)。
    /// </summary>
    public MerchantInventory? Inventory => NMerchantRoom.Instance?.Room.GetLocalInventory();

    /// <summary>まだ売り切れていない(購入済みで無くなっていない)出品。ショップの外ではnullなので空になる。</summary>
    public IEnumerable<MerchantEntry> StockedEntries => Inventory?.AllEntries.Where(e => e.IsStocked) ?? Enumerable.Empty<MerchantEntry>();

    /// <summary>現在の所持ゴールドで、まだ何か買えるものが残っているか。</summary>
    public bool CanAffordAnything => StockedEntries.Any(e => e.EnoughGold);
}
