using ChattyVakuusEarring.ChattyVakuusEarringCode.Chatter.Observer;
using ChattyVakuusEarring.ChattyVakuusEarringCode.Chatter.Speaker;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models.Relics;

namespace ChattyVakuusEarring.ChattyVakuusEarringCode.Chatter.Detectors.Relics;

/// <summary>
/// 「パエルの涙が泣いてますよ」。エナジーを1つも残せずにターンを終えた(＝パエルの涙が発動しなかった)時の一言。
/// </summary>
/// <remarks>
/// どうしても使いたいカードがあったかどうかは考慮しない(「指示厨」なので容赦しない、という設計メモの通り)。
/// パエルの涙自身は発動条件・タイミングが全て公開状態(<c>Owner.PlayerCombatState.Energy</c>)から追えるので、
/// <see cref="WastedBlockRelicComplaintDetector"/>と違いHarmonyパッチは不要。
/// </remarks>
public sealed class PaelsTearsComplaintDetector : PlayDetector
{
    private const string Topic = "PAELS_TEARS_COMPLAINT";

    public override DetectorTrigger Triggers => DetectorTrigger.PlayerEndedTurn;

    public override bool ShouldActivate(PlayObserver observer) => observer.HasRelic<PaelsTears>();

    public override Utterance? Detect(PlayObserver observer, DetectorTrigger trigger)
    {
        if (observer.Energy > 0)
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
            Probability = 0.35f,
            Tags = new[] { UtteranceTag.RelicComplaint, UtteranceTag.Energy },
        };
    }
}
