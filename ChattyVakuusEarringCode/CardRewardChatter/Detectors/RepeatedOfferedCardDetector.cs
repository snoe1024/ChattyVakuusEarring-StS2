using System.Linq;
using ChattyVakuusEarring.ChattyVakuusEarringCode.Chatter.Speaker;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;

namespace ChattyVakuusEarring.ChattyVakuusEarringCode.CardRewardChatter.Detectors;

/// <summary>
/// アンコモン以上のレア度のカードが、過去に何度も(3回以上)報酬として提示されているのに一度も
/// ピックされたことがなく、それがまた選択肢に出てきた時の一言。
/// </summary>
/// <remarks>
/// こちらは<see cref="RepeatedSkippedCardDetector"/>と違い「他のカードに競り負けたか」は問わない
/// (2026-09、ユーザー要望)。3回以上も出会いながら一度も選ばれていないこと自体が十分に煽れる材料
/// (コモンはそもそも選択肢に競合が少なく頻繁に見送られがちなので対象から除外している)。
/// </remarks>
public sealed class RepeatedOfferedCardDetector : CardRewardDetector
{
    private const string Topic = "CARD_REWARD_REPEATEDLY_OFFERED";

    private const int Priority = 8;

    private const int MinPastOffers = 3;

    public override CardRewardDetectorTrigger Triggers => CardRewardDetectorTrigger.Shown;

    public override CardRewardProposal? Detect(CardRewardObserver observer, CardRewardDetectorTrigger trigger)
    {
        CardModel? repeated = observer.Cards.FirstOrDefault(card =>
            card.Rarity is CardRarity.Uncommon or CardRarity.Rare
            && IsRepeatedlyOfferedAndNeverPicked(observer, card));
        if (repeated == null)
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

        return new CardRewardProposal(utterance, repeated);
    }

    private static bool IsRepeatedlyOfferedAndNeverPicked(CardRewardObserver observer, CardModel card)
    {
        var pastChoices = observer.PastChoicesOf(card).ToList();
        return pastChoices.Count >= MinPastOffers && pastChoices.All(c => !c.WasPicked);
    }
}
