using System.Linq;
using ChattyVakuusEarring.ChattyVakuusEarringCode.Chatter.Speaker;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;

namespace ChattyVakuusEarring.ChattyVakuusEarringCode.CardRewardChatter.Detectors;

/// <summary>
/// 直近の報酬で未アップグレードで選んだカードが、今度はアップグレード版で選択肢に出てきた時の一言
/// (逆に「あの時取ったカード、強化版だったらもっと良かったのに」的な文脈を想定)。
/// 「昔すぎることを掘り返されても覚えていない」ため、遡る範囲を直近5回分の報酬に限定している。
/// <see cref="CardRewardObserver.RecentPastChoicesOf"/>参照。
/// </summary>
public sealed class UpgradedRepeatDetector : CardRewardDetector
{
    private const string Topic = "CARD_REWARD_SEEN_BEFORE_UPGRADED";

    private const int Priority = 8;

    public override CardRewardDetectorTrigger Triggers => CardRewardDetectorTrigger.Shown;

    public override CardRewardProposal? Detect(CardRewardObserver observer, CardRewardDetectorTrigger trigger)
    {
        CardModel? upgradedRepeat = observer.Cards.FirstOrDefault(card =>
            card.EnergyCost.Canonical is not 0 && card.IsUpgraded
            && observer.RecentPastChoicesOf(card).Any(choice => choice.WasPicked && !choice.WasUpgraded));
        if (upgradedRepeat == null)
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
            Probability = 0.85f,
            Priority = Priority,
            Tags = new[] { UtteranceTag.CardReward },
        };

        return new CardRewardProposal(utterance, upgradedRepeat);
    }
}
