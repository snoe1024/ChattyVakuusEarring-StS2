using System.Linq;
using ChattyVakuusEarring.ChattyVakuusEarringCode.Chatter.Observer;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models.Potions;

namespace ChattyVakuusEarring.ChattyVakuusEarringCode.Chatter.Detectors.Potions;

/// <summary>
/// 「フレックスポーションが泣いてますよ」。付与した筋力を活かせる、ダメージを与えるカードが
/// 相手の手札に1枚も無い状況での使用への一言。
/// </summary>
/// <remarks>
/// 「ダメージを与えるか」ではなく「筋力の恩恵を受けるか」(<see cref="PlayObserver.AttackDamageBenefitsFromStrength"/>)
/// で見る。オスティ(Osty)由来のダメージ(<c>OstyDamageVar</c>、または<c>Unleash</c>・<c>Protector</c>のような
/// <c>IsFromOsty</c>付き<c>CalculatedDamageVar</c>)は、持ち主自身の筋力(フレックスポーションで得るもの)が
/// 乗らないため、そちらしか手札に無い場合はフレックスポーションが無駄だったとみなす。
/// </remarks>
public sealed class FlexPotionMisuseDetector : PotionMisuseDetector<FlexPotion>
{
    protected override float Probability => 0.6f;

    protected override string Topic => "FLEX_POTION_MISUSE";

    protected override bool WasWasted(PlayObserver observer, FlexPotion potion, Creature target) =>
        PlayObserver.HandOf(target).All(c => !PlayObserver.AttackDamageBenefitsFromStrength(c));
}
