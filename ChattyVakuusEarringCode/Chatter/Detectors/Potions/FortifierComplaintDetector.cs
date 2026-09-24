using ChattyVakuusEarring.ChattyVakuusEarringCode.Chatter.Observer;
using ChattyVakuusEarring.ChattyVakuusEarringCode.Chatter.Speaker;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Potions;

namespace ChattyVakuusEarring.ChattyVakuusEarringCode.Chatter.Detectors.Potions;

/// <summary>
/// 「防御強化薬が泣いてますよ」。現在のブロックを3倍にすれば18以上のダメージを防げたのに、
/// 使わずにターンが終わった時の一言。
/// </summary>
/// <remarks>
/// 防御強化薬は現在のブロックを3倍にする(<c>OnUse</c>実装は「2倍を加算」なので合計3倍)。
/// この倍率自体は<c>DynamicVars</c>を持たない固定の効果なので、ここでは定数として扱う。
/// </remarks>
public sealed class FortifierComplaintDetector : PlayDetector
{
    /// <summary>防御強化薬の効果(現在のブロックが何倍になるか)。</summary>
    private const int Multiplier = 3;

    /// <summary>「この差額以上防げたはず」とみなすダメージの閾値。</summary>
    private const int PreventableDamageThreshold = 18;

    public override DetectorTrigger Triggers => DetectorTrigger.TurnEnded;

    public override Utterance? Detect(PlayObserver observer, DetectorTrigger trigger)
    {
        if (observer.WasVakuuTurn || observer.Block <= 0 || !observer.HasUnusedPotion<Fortifier>())
        {
            return null;
        }

        int extraBlock = observer.Block * (Multiplier - 1);
        int unblocked = observer.EstimateIncomingAttackDamage() - observer.Block;
        if (unblocked < PreventableDamageThreshold || extraBlock < PreventableDamageThreshold)
        {
            return null;
        }

        LocString? line = SpeechTable.Pick($"{ModelDb.Potion<Fortifier>().Id.Entry}_COMPLAINT");
        if (line == null)
        {
            return null;
        }

        return new Utterance
        {
            Line = line,
            Probability = 0.6f,
            Tags = new[] { UtteranceTag.PotionComplaint, UtteranceTag.Blocking },
        };
    }
}
