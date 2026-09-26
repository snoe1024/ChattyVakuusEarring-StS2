using ChattyVakuusEarring.ChattyVakuusEarringCode.Chatter.Observer;
using ChattyVakuusEarring.ChattyVakuusEarringCode.Chatter.Speaker;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models.Powers;

namespace ChattyVakuusEarring.ChattyVakuusEarringCode.Chatter.Detectors;

/// <summary>
/// 持ち主が敵の「筋力」を下げた(負のスタックの<c>StrengthPower</c>を付与した)時の一言。
/// </summary>
/// <remarks>
/// <c>StrengthPower.Type</c>は常に<c>PowerType.Buff</c>を返す(正負どちらのスタックも同じ型)ため、
/// <see cref="DebuffAppliedDetector{TPower}"/>(<c>Power.Type == Debuff</c>前提ではなく、単純に型一致で
/// 判定している)とは別に、負のスタックであることまで見るこの専用Detectorが要る。
/// </remarks>
public sealed class EnemyStrengthDownDetector : PlayDetector
{
    private const string Topic = "ENEMY_STRENGTH_DOWN";

    public override DetectorTrigger Triggers => DetectorTrigger.DebuffApplied;

    public override Utterance? Detect(PlayObserver observer, DetectorTrigger trigger)
    {
        if (observer.IsFirstTurn || observer.LastDebuffApplied is not { Power: StrengthPower, Amount: < 0 })
        {
            return null;
        }

        LocString? line = SpeechTable.Pick(Topic);
        if (line == null)
        {
            return null;
        }

        return new Utterance
        {
            Line = line,
            Probability = 0.25f,
            Tags = new[] { UtteranceTag.Attacking },
        };
    }
}
