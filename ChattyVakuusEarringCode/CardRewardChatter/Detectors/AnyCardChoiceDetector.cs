using ChattyVakuusEarring.ChattyVakuusEarringCode.Chatter.Speaker;
using MegaCrit.Sts2.Core.Localization;

namespace ChattyVakuusEarring.ChattyVakuusEarringCode.CardRewardChatter.Detectors;

/// <summary>
/// アタックポーション等の「カードを1枚選ぶ」画面の中身を問わず、画面の端から低確率で挟む汎用的な一言
/// (<see cref="AnyCardRewardDetector"/>のこの画面版)。特定のカードを名指ししないので、吹き出しは
/// カードの座標ではなく画面の端から出す(<c>AnchorCard</c>はnull)。
/// </summary>
public sealed class AnyCardChoiceDetector : CardRewardDetector
{
    private const string Topic = "CARD_CHOICE_GENERIC";

    public override CardRewardDetectorTrigger Triggers => CardRewardDetectorTrigger.ChooseACardShown;

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
