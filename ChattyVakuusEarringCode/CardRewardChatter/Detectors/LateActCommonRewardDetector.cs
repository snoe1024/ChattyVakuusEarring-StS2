using System.Linq;
using ChattyVakuusEarring.ChattyVakuusEarringCode.Chatter.Speaker;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Localization;

namespace ChattyVakuusEarring.ChattyVakuusEarringCode.CardRewardChatter.Detectors;

/// <summary>
/// Act3以降なのに、カード報酬が全て未アップグレードのコモンカードだった時に嘲笑う。
/// </summary>
public sealed class LateActCommonRewardDetector : CardRewardDetector
{
    private const string Topic = "CARD_REWARD_LATE_ACT_COMMON";

    private const int Priority = 6;

    public override CardRewardDetectorTrigger Triggers => CardRewardDetectorTrigger.Shown;

    public override CardRewardProposal? Detect(CardRewardObserver observer, CardRewardDetectorTrigger trigger)
    {
        // Act1=0, Act2=1, Act3=2(IRunState.CurrentActIndexのXMLドキュメント参照)。
        if (observer.Owner.RunState.CurrentActIndex < 2
            || observer.Cards.Count == 0
            || !observer.Cards.All(c => c.Rarity == CardRarity.Common && !c.IsUpgraded))
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
            Probability = 0.3f,
            Priority = Priority,
            Tags = new[] { UtteranceTag.CardReward },
        };

        // 選択肢全体についての言及なので、特定の1枚を名指ししない(画面端から)。
        return new CardRewardProposal(utterance, null);
    }
}
