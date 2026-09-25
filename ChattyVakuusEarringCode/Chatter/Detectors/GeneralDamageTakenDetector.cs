using ChattyVakuusEarring.ChattyVakuusEarringCode.Chatter.Observer;
using ChattyVakuusEarring.ChattyVakuusEarringCode.Chatter.Speaker;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Localization;

namespace ChattyVakuusEarring.ChattyVakuusEarringCode.Chatter.Detectors;

/// <summary>
/// 敵からダメージを受けた時の、汎用的な一言(中確率の相槌枠)。
/// </summary>
/// <remarks>
/// <see cref="HeavyHitDetector"/>・<see cref="LowHpDetector"/>等、より具体的な指摘があればそちらを
/// 優先させたいので優先度を下げてある。最初のターン(ヴァクーの代打ちとその余波の敵ターンまで)は
/// <see cref="PlayObserver.IsFirstTurn"/>で除外(その場面は他のDetectorが拾う)。
/// </remarks>
public sealed class GeneralDamageTakenDetector : PlayDetector
{
    private const string Topic = "GENERAL_DAMAGE_TAKEN";

    private const int NoticePriority = -5;

    public override DetectorTrigger Triggers => DetectorTrigger.DamageTaken;

    public override Utterance? Detect(PlayObserver observer, DetectorTrigger trigger)
    {
        if (observer.IsFirstTurn)
        {
            return null;
        }

        var taken = observer.LastDamageTaken;
        if (taken?.Dealer?.Side != CombatSide.Enemy || taken.Result.UnblockedDamage <= 0)
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
            Probability = 0.4f,
            Priority = NoticePriority,
            Tags = new[] { UtteranceTag.Danger },
        };
    }
}
