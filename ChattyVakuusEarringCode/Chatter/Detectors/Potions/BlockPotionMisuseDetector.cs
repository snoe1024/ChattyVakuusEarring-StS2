using System;
using ChattyVakuusEarring.ChattyVakuusEarringCode.Chatter.Observer;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models.Potions;
using MegaCrit.Sts2.Core.Models.Relics;

namespace ChattyVakuusEarring.ChattyVakuusEarringCode.Chatter.Detectors.Potions;

/// <summary>
/// 「ブロックポーションが泣いてますよ」。使う前から既にブロックだけで想定被弾を防げていた
/// (=もらったブロック分がまるまる不要だった)、または致死にならない軽微な被弾(2以下)で
/// 済んでいたはずの状況での使用への一言。
/// </summary>
/// <remarks>
/// <para>
/// 被弾予測を使うため、効果の対象が持ち主自身の時だけ判定する(マルチプレイで仲間に投げた場合は
/// 判定しない。<see cref="PotionMisuseDetector{TPotion}"/>のドキュメント参照)。
/// </para>
/// <para>
/// 「防げたはずの被弾が0」ではなく「2以下」まで無駄判定を広げているが、溶岩のランプ(<see cref="LavaLamp"/>、
/// 戦闘中無被弾なら報酬をアップグレードするレリック)持ちは1点の被弾でもボーナスを崩すので対象外にする
/// (＝溶岩のランプ所持時は、従来通り完全に0でなければ無駄とはみなさない)。致死になる被弾を無理して
/// 受けさせる話ではないので、防げたはずの被弾が現在HP以上(＝防がなければ死んでいた)場合も対象外にする。
/// </para>
/// </remarks>
public sealed class BlockPotionMisuseDetector : PotionMisuseDetector<BlockPotion>
{
    /// <summary>溶岩のランプを持っていない時、この値以下の被弾なら「身体で受ければよかった」とみなす。</summary>
    private const int TolerableChipDamage = 2;

    protected override float Probability => 0.6f;

    protected override string Topic => "BLOCK_POTION_MISUSE";

    protected override bool WasWasted(PlayObserver observer, BlockPotion potion, Creature target)
    {
        if (target != observer.OwnerCreature)
        {
            return false;
        }

        int blockBeforePotion = target.Block - potion.DynamicVars.Block.IntValue;
        int unblockedBeforePotion = Math.Max(0, observer.EstimateIncomingAttackDamage() - blockBeforePotion);
        if (unblockedBeforePotion <= 0)
        {
            return true;
        }

        if (observer.HasRelic<LavaLamp>())
        {
            return false;
        }

        bool wouldHaveSurvived = unblockedBeforePotion < target.CurrentHp;
        return wouldHaveSurvived && unblockedBeforePotion <= TolerableChipDamage;
    }
}
