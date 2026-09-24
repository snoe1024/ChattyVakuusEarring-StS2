using ChattyVakuusEarring.ChattyVakuusEarringCode.Chatter.Observer;
using ChattyVakuusEarring.ChattyVakuusEarringCode.Chatter.Speaker;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models.Relics;

namespace ChattyVakuusEarring.ChattyVakuusEarringCode.Chatter.Detectors.Relics;

/// <summary>
/// 「黄金の印章が泣いてますよ」。ゴールドが足りず、ターン開始時にエナジーをもらえなかった時の一言。
/// </summary>
/// <remarks>
/// 黄金の印章はターン開始時、ゴールドが既定の必要量(<c>DynamicVars.Gold</c>)以上あれば自動でそれを支払い
/// エナジーを得る。足りない時は静かに何も起きないので、「発動しなかった」こと自体は公開状態
/// (<c>Owner.Gold</c>とレリックの<c>DynamicVars</c>)だけで判定でき、Harmonyパッチは不要。
/// 戦闘中1回だけ言う(毎ターン同じ理由で言われ続けるとうるさいため)。
/// </remarks>
public sealed class SealOfGoldComplaintDetector : PlayDetector
{
    private const string Topic = "SEAL_OF_GOLD_COMPLAINT";

    private bool _proposed;

    public override DetectorTrigger Triggers => DetectorTrigger.PlayerTurnStarted;

    public override bool ShouldActivate(PlayObserver observer) => observer.HasRelic<SealOfGold>();

    public override Utterance? Detect(PlayObserver observer, DetectorTrigger trigger)
    {
        if (_proposed)
        {
            return null;
        }

        SealOfGold relic = observer.GetRelic<SealOfGold>()!;
        if (observer.Owner.Gold >= relic.DynamicVars.Gold.IntValue)
        {
            return null;
        }

        LocString? line = SpeechTable.Pick(Topic);
        if (line == null)
        {
            return null;
        }

        _proposed = true;
        return new Utterance
        {
            Line = line,
            Probability = 0.8f,
            Tags = new[] { UtteranceTag.RelicComplaint, UtteranceTag.Energy },
        };
    }
}
