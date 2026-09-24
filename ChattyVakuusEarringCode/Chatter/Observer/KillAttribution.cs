using MegaCrit.Sts2.Core.Combat.History.Entries;
using MegaCrit.Sts2.Core.Entities.Players;

namespace ChattyVakuusEarring.ChattyVakuusEarringCode.Chatter.Observer;

/// <summary>
/// ダメージの記録から、「誰(どのプレイヤー)が倒したか」を割り出す。
/// </summary>
internal static class KillAttribution
{
    /// <summary>
    /// 攻撃した側のクリーチャー → その手下(Ostyなど、プレイヤーのペット) → ダメージを与えたカードの持ち主、の順に
    /// プレイヤーを探す。どれでも分からなければnull。
    /// </summary>
    public static Player? FindKiller(DamageReceivedEntry entry)
    {
        return entry.Dealer?.Player
               ?? entry.Dealer?.PetOwner
               ?? entry.CardSource?.Owner;
    }
}
