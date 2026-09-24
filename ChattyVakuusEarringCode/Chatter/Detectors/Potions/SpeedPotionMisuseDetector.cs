using System.Linq;
using ChattyVakuusEarring.ChattyVakuusEarringCode.Chatter.Observer;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models.Potions;

namespace ChattyVakuusEarring.ChattyVakuusEarringCode.Chatter.Detectors.Potions;

/// <summary>
/// 「スピードポーションが泣いてますよ」。付与した敏捷を活かせる、ブロックを生むカードが
/// 相手の手札に1枚も無い状況での使用への一言。
/// </summary>
public sealed class SpeedPotionMisuseDetector : PotionMisuseDetector<SpeedPotion>
{
    protected override float Probability => 0.6f;

    protected override string Topic => "SPEED_POTION_MISUSE";

    protected override bool WasWasted(PlayObserver observer, SpeedPotion potion, Creature target) =>
        PlayObserver.HandOf(target).All(c => !PlayObserver.CardProvidesBlock(c));
}
