using ChattyVakuusEarring.ChattyVakuusEarringCode.Chatter.Speaker;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;

namespace ChattyVakuusEarring.ChattyVakuusEarringCode.CardRewardChatter.Detectors;

/// <summary>
/// パワーポーション等の「カードを1枚選ぶ」で悪魔化が登場したときの確定台詞。
/// </summary>
public sealed class DemonFormChoiceDetector : CardRewardDetector
{
    private const string Topic = "CARD_CHOICE_DEMON_FORM";

    public override CardRewardDetectorTrigger Triggers => CardRewardDetectorTrigger.ChooseACardShown;

    public override CardRewardProposal? Detect(CardRewardObserver observer, CardRewardDetectorTrigger trigger)
    {
        CardModel? demonForm = observer.Cards.FirstOrDefault(c => c is DemonForm);
        if (demonForm == null)
        {
            return null;
        }
        
        LocString? line = SpeechTable.Pick(Topic);
        if (line == null)
        {
            return null;
        }

        var utterance = new Utterance
        {
            Line = line,
            Force = true,
            Tags = new[] { UtteranceTag.CardReward },
        };

        return new CardRewardProposal(utterance, demonForm);
    }
}
