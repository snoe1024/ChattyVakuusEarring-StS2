using ChattyVakuusEarring.ChattyVakuusEarringCode.Chatter.Observer;
using ChattyVakuusEarring.ChattyVakuusEarringCode.Chatter.Speaker;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;

namespace ChattyVakuusEarring.ChattyVakuusEarringCode.Chatter.Detectors;

/// <summary>
/// 生成されたカードが、コスト増加効果(レリック「とげ付きガントレット」のパワー限定コスト増加、
/// パワー「借り物の時間」のコスト増加等)さえ無ければ0コストで使えたはずなのに、実際のコストが0を超え
/// エナジー不足でプレイできない状態になっている時の一言。
/// </summary>
public sealed class GeneratedCardUnaffordableDetector : PlayDetector
{
    private const string Topic = "GENERATED_CARD_UNAFFORDABLE";

    public override DetectorTrigger Triggers => DetectorTrigger.CardGenerated;

    public override Utterance? Detect(PlayObserver observer, DetectorTrigger trigger)
    {
        CardModel? card = observer.LastGeneratedCard;
        if (card == null || card.EnergyCost.CostsX)
        {
            return null;
        }

        // グローバルフック(レリック・パワー等)を含めない、このカード固有の実効コスト。
        if (card.EnergyCost.GetWithModifiers(CostModifiers.Local) != 0)
        {
            return null;
        }

        if (card.CanPlay(out var reason, out _) || (reason & UnplayableReason.EnergyCostTooHigh) == 0)
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
            Probability = 1f,
            Tags = new[] { UtteranceTag.CardDrawn },
        };
    }
}
