using ChattyVakuusEarring.ChattyVakuusEarringCode.Chatter.Observer;
using ChattyVakuusEarring.ChattyVakuusEarringCode.Chatter.Speaker;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;

namespace ChattyVakuusEarring.ChattyVakuusEarringCode.Chatter.Detectors;

/// <summary>
/// 「酷い手札ですねぇ…」。配られた手札が、デッキ全体に比べてレア度の面で極端に低いと言う。
/// </summary>
/// <remarks>
/// <para>
/// 評価は<see cref="CardRarityScore"/>の値の「1枚あたりの平均」で、<b>手札の平均が、デッキ全体の平均の
/// <see cref="BadHandRatio"/>倍以下</b>の時に酷い手札とみなす(デッキが強いほど、同じ手札でも酷く見える)。
/// </para>
/// <para>
/// 最初のターンは、ヴァクーが代打ちを始める直前(<see cref="DetectorTrigger.VakuuTurnStarted"/>)の
/// 「配られたままの手札」を見る。2ターン目以降は<see cref="DetectorTrigger.PlayerTurnStarted"/>で見る。
/// 最初のターンの<see cref="DetectorTrigger.PlayerTurnStarted"/>は、代打ちで手札が減った後なので、
/// 同じターンを2回評価しないようにして飛ばす(ヴァクーの代打ちの結果への講評は<see cref="FirstTurnReviewDetector"/>の仕事)。
/// </para>
/// <para>
/// 呪いだらけの手札は<see cref="CurseHandDetector"/>が(より高い優先度で)先に判断するので、
/// そちらが喋った場合、こちらは間隔ルールで黙る。
/// </para>
/// </remarks>
public sealed class HandQualityDetector : PlayDetector
{
    private const string Topic = "BAD_HAND";

    /// <summary>手札の平均評価値が、デッキの平均評価値のこの倍率以下なら酷い手札とする。</summary>
    private const double BadHandRatio = 0.2;

    public override DetectorTrigger Triggers =>
        DetectorTrigger.VakuuTurnStarted | DetectorTrigger.PlayerTurnStarted;

    public override Utterance? Detect(PlayObserver observer, DetectorTrigger trigger)
    {
        if (!TryStartEvaluationForTurn(observer))
        {
            return null;
        }

        double deckAverage = AverageScore(observer.Deck);
        if (observer.Hand.Count == 0 || deckAverage <= 0)
        {
            return null;
        }

        if (AverageScore(observer.Hand) > deckAverage * BadHandRatio)
        {
            return null;
        }

        LocString? line = SpeechTable.Pick(Topic);
        if (line == null)
        {
            return null;
        }

        return new Utterance
        {
            Line = line,
            Probability = 0.8f,
            Tags = new[] { UtteranceTag.HandQuality },
        };
    }

    private static int RarityScore(CardModel card)
    {
        return card.Rarity switch
        {
            CardRarity.Ancient => 12,
            CardRarity.Rare => 10,
            CardRarity.Uncommon => 3,
            CardRarity.Common => 2,
            CardRarity.Basic => 1, // スターター
            CardRarity.Curse => -1,
            CardRarity.Status => -1,
            CardRarity.Event => -1,
            CardRarity.Quest => -1,
            _ => 2, // Token / None
        };
    }

    /// <summary>カード1枚あたりの平均評価値。カードが0枚なら0。</summary>
    private static double AverageScore(IReadOnlyList<CardModel> cards)
    {
        return cards.Count == 0 ? 0 : cards.Sum(RarityScore) / (double)cards.Count;
    }
}
