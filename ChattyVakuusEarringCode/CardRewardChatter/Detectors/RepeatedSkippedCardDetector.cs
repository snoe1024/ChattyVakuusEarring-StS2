using System.Linq;
using ChattyVakuusEarring.ChattyVakuusEarringCode.Chatter.Speaker;
using MegaCrit.Sts2.Core.Localization;

namespace ChattyVakuusEarring.ChattyVakuusEarringCode.CardRewardChatter.Detectors;

/// <summary>
/// 今回提示された全カードが、それぞれ過去に報酬として提示されたことがあり、かつその回(1回の報酬全体)では
/// 結局どのカードもピックされなかった(=競合に負けたのではなく、報酬そのものが丸ごと見送られた)場合の一言。
/// <see cref="CardRewardObserver.WasOfferedAndFullySkipped"/>参照。
/// </summary>
/// <remarks>
/// 単に「そのカードが一度でも見送られたことがある」だけでは、単に他のカードに競り負けただけの場合まで
/// 「取らなかったですね」と煽ることになり不当(2026-09、ユーザー指摘)。今回の選択肢全部が、それぞれの
/// 出現時に「何も選ばれなかった」という意味で純粋に不要とされた過去を持つ場合だけに絞っている。
/// </remarks>
public sealed class RepeatedSkippedCardDetector : CardRewardDetector
{
    private const string Topic = "CARD_REWARD_SEEN_BEFORE_SKIPPED";

    private const int Priority = 8;

    public override CardRewardDetectorTrigger Triggers => CardRewardDetectorTrigger.Shown;

    public override CardRewardProposal? Detect(CardRewardObserver observer, CardRewardDetectorTrigger trigger)
    {
        if (observer.Cards.Count == 0 || !observer.Cards.All(observer.WasOfferedAndFullySkipped))
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

        // 選択肢全体についての言及なので、特定の1枚を名指ししない(画面端から)。
        return new CardRewardProposal(utterance, null);
    }
}
