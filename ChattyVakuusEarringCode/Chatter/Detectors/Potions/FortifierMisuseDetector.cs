using System;
using ChattyVakuusEarring.ChattyVakuusEarringCode.Chatter.Observer;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models.Potions;

namespace ChattyVakuusEarring.ChattyVakuusEarringCode.Chatter.Detectors.Potions;

/// <summary>
/// 「防御強化薬が泣いてますよ」。元のブロック値が低すぎる(3以下)か、3倍にしたことで新たに防げる
/// ようになったダメージが小さすぎる(3以下)状況での使用への一言。
/// </summary>
/// <remarks>
/// <para>
/// 防御強化薬は現在のブロックを3倍にする(<c>OnUse</c>実装は「2倍を加算」なので合計3倍。<c>DynamicVars</c>を
/// 持たない固定倍率なので、使用後のブロックを3で割って元の値を逆算する)。
/// </para>
/// <para>
/// 被弾予測を使う後半の判定は、効果の対象が持ち主自身の時だけ行う(マルチプレイで仲間に投げた場合は、
/// 「元のブロック値が低すぎる」の方だけで判定する。<see cref="PotionMisuseDetector{TPotion}"/>のドキュメント参照)。
/// </para>
/// </remarks>
public sealed class FortifierMisuseDetector : PotionMisuseDetector<Fortifier>
{
    /// <summary>元のブロック値がこれ以下なら、倍率に関わらず無駄とみなす。</summary>
    private const int MinOriginalBlock = 3;

    /// <summary>新たに防げるようになったダメージがこれ以下なら無駄とみなす。</summary>
    private const int MinPreventedDamage = 3;

    protected override float Probability => 0.6f;

    protected override string Topic => "FORTIFIER_MISUSE";

    protected override bool WasWasted(PlayObserver observer, Fortifier potion, Creature target)
    {
        int newBlock = target.Block;
        int originalBlock = newBlock / 3;
        if (originalBlock <= MinOriginalBlock)
        {
            return true;
        }

        if (target != observer.OwnerCreature)
        {
            return false;
        }

        int incoming = observer.EstimateIncomingAttackDamage();
        int unblockedBefore = Math.Max(0, incoming - originalBlock);
        int unblockedAfter = Math.Max(0, incoming - newBlock);
        return unblockedBefore - unblockedAfter <= MinPreventedDamage;
    }
}
