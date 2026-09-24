using ChattyVakuusEarring.ChattyVakuusEarringCode.Chatter.Observer;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models.Potions;

namespace ChattyVakuusEarring.ChattyVakuusEarringCode.Chatter.Detectors.Potions;

/// <summary>
/// 「ブラッドポーションが泣いてますよ」。対象の体力が既に満タンで、回復量が丸ごと無駄になった時の一言。
/// </summary>
/// <remarks>
/// 使用後の<c>CurrentHp</c>は既に回復が反映済み(かつ最大HPで頭打ち)なので、使用前のHPを直接は読めない。
/// そこで、本家の<c>BloodPotion.OnUse</c>と同じ式(<c>MaxHp * HealPercent / 100</c>)で回復量を逆算し、
/// 「使用後のHP − 回復量」が最大HP以上(＝頭打ちが発生する前から既に満タンだった)かどうかで判定する。
/// 頭打ちが発生した(＝多少なりともHPが減っていた)場合はこの式の結果が最大HP未満になるので、誤検知はしない。
/// </remarks>
public sealed class BloodPotionMisuseDetector : PotionMisuseDetector<BloodPotion>
{
    protected override float Probability => 0.6f;

    protected override string Topic => "BLOOD_POTION_MISUSE";

    protected override bool WasWasted(PlayObserver observer, BloodPotion potion, Creature target)
    {
        decimal healPercent = potion.DynamicVars["HealPercent"].BaseValue;
        decimal nominalHeal = target.MaxHp * healPercent / 100m;
        return target.CurrentHp - nominalHeal >= target.MaxHp;
    }
}
