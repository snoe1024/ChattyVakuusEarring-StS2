using System.Linq;
using ChattyVakuusEarring.ChattyVakuusEarringCode.Chatter.Speaker;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;

namespace ChattyVakuusEarring.ChattyVakuusEarringCode.CardRewardChatter.Detectors;

/// <summary>
/// カード報酬に「悪魔化(DemonForm)」が含まれている時、そのカードの近くから「ぜひ取りましょう」的な一言。
/// </summary>
public sealed class DemonFormRewardDetector : CardRewardDetector
{
    private const string Topic = "CARD_REWARD_DEMON_FORM";

    public override CardRewardDetectorTrigger Triggers => CardRewardDetectorTrigger.Shown;

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
