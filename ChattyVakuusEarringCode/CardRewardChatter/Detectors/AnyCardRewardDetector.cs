using ChattyVakuusEarring.ChattyVakuusEarringCode.Chatter.Speaker;
using MegaCrit.Sts2.Core.Localization;

namespace ChattyVakuusEarring.ChattyVakuusEarringCode.CardRewardChatter.Detectors;

/// <summary>
/// カード報酬の中身を問わず、画面の端から低確率で挟む汎用的な一言(手堅く何も言わない場面を減らすための雑談枠)。
/// 特定のカードを名指ししないので、吹き出しはカードの座標ではなく画面の端から出す(<c>AnchorCard</c>はnull)。
/// </summary>
public sealed class AnyCardRewardDetector : CardRewardDetector
{
    private const string Topic = "CARD_REWARD_GENERIC";

    public override CardRewardDetectorTrigger Triggers => CardRewardDetectorTrigger.Shown;

    public override CardRewardProposal? Detect(CardRewardObserver observer, CardRewardDetectorTrigger trigger)
    {
        LocString? line = SpeechTable.Pick(Topic);
        if (line == null)
        {
            return null;
        }

        var utterance = new Utterance
        {
            Line = line,
            Probability = 0.3f,
            Tags = new[] { UtteranceTag.CardReward },
        };

        return new CardRewardProposal(utterance, null);
    }
}
