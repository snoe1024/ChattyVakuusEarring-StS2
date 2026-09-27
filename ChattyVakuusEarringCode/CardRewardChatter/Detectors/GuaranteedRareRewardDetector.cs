using ChattyVakuusEarring.ChattyVakuusEarringCode.Chatter.Speaker;
using MegaCrit.Sts2.Core.Localization;

namespace ChattyVakuusEarring.ChattyVakuusEarringCode.CardRewardChatter.Detectors;

/// <summary>
/// カード報酬が全てレア確定の時(ボス戦の報酬、またはホワイトスターレリック所持時のエリート戦での
/// 追加報酬)の一言。判定は<see cref="CardRewardObserver.IsGuaranteedRareReward"/>参照。
/// </summary>
/// <remarks>
/// 「レア確定かどうか」は画面が表示されるタイミング自体は同じ(<see cref="CardRewardDetectorTrigger.Shown"/>)
/// なので、専用のTriggerは増やさず<see cref="CardRewardObserver"/>のプロパティをこの<see cref="Detect"/>内で
/// if判定する方針にした(2026-09、ユーザー確認。<see cref="CardRewardDetectorTrigger"/>のXMLドキュメント参照)。
/// </remarks>
public sealed class GuaranteedRareRewardDetector : CardRewardDetector
{
    private const string Topic = "CARD_REWARD_GUARANTEED_RARE";

    /// <summary>任意のカード報酬全般についての一言(<see cref="AnyCardRewardDetector"/>)より優先する。</summary>
    private const int Priority = 5;

    public override CardRewardDetectorTrigger Triggers => CardRewardDetectorTrigger.Shown;

    public override CardRewardProposal? Detect(CardRewardObserver observer, CardRewardDetectorTrigger trigger)
    {
        if (!observer.IsGuaranteedRareReward)
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
            Probability = 0.7f,
            Priority = Priority,
            Tags = new[] { UtteranceTag.CardReward },
        };

        // 全カードがレアで、特定の1枚を名指しする理由が無いので画面端から(AnchorCardはnull)。
        return new CardRewardProposal(utterance, null);
    }
}
