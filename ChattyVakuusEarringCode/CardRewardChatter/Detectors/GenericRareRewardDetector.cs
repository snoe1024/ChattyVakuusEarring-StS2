using System.Linq;
using ChattyVakuusEarring.ChattyVakuusEarringCode.Chatter.Speaker;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;

namespace ChattyVakuusEarring.ChattyVakuusEarringCode.CardRewardChatter.Detectors;

/// <summary>
/// レア確定ではない通常の報酬に、たまたまレアカードが出現した時に珍しがる。
/// </summary>
/// <remarks>
/// レア確定の報酬(<see cref="GuaranteedRareRewardDetector"/>参照)ではレアが出ても当たり前なので対象外にする。
/// </remarks>
public sealed class GenericRareRewardDetector : CardRewardDetector
{
    private const string Topic = "CARD_REWARD_GENERIC_RARE";

    private const int Priority = 6;

    public override CardRewardDetectorTrigger Triggers => CardRewardDetectorTrigger.Shown;

    public override CardRewardProposal? Detect(CardRewardObserver observer, CardRewardDetectorTrigger trigger)
    {
        if (observer.IsGuaranteedRareReward)
        {
            return null;
        }

        CardModel? rare = observer.Cards.FirstOrDefault(c => c.Rarity == CardRarity.Rare);
        if (rare == null)
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
            Probability = 0.4f,
            Priority = Priority,
            Tags = new[] { UtteranceTag.CardReward },
        };

        return new CardRewardProposal(utterance, rare);
    }
}
