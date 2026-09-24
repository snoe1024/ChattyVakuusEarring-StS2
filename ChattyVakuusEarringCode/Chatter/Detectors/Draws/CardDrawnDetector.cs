using System.Linq;
using ChattyVakuusEarring.ChattyVakuusEarringCode.Chatter.Observer;
using ChattyVakuusEarring.ChattyVakuusEarringCode.Chatter.Speaker;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

namespace ChattyVakuusEarring.ChattyVakuusEarringCode.Chatter.Detectors.Draws;

/// <summary>
/// ターン中に引いた、重要だが今は手札に置いておくしかないカードへの一言の共通実装。
/// </summary>
/// <remarks>
/// 「今プレイできない」を全種共通の前提とし、各具象クラスは「重要」の中身(パワー/非スターターのエセリアル/
/// ブロックが必要な場面でのブロックカード、等)だけを<see cref="Matches"/>で判定すればよい。
/// ただし「プレイできない」は<see cref="CardModel.CanPlay()"/>をそのまま信じず、理由がエナジー不足
/// (<see cref="UnplayableReason.EnergyCostTooHigh"/>)だけの場合は、手札に今すぐプレイできるエナジー獲得カード
/// (<see cref="PlayObserver.EnergyCardProvide"/>)が無いかも見る。あれば、先にそれを打てば実質プレイできる
/// ので「詰んでいる」とは言えず、対象外にする(<see cref="IsStuckUnplayable"/>)。
/// </remarks>
public abstract class CardDrawnDetector : PlayDetector
{
    public override DetectorTrigger Triggers => DetectorTrigger.CardDrawn;

    /// <summary>この一言を提案する確率。</summary>
    protected abstract float Probability { get; }

    /// <summary>台詞のローカライズトピック。</summary>
    protected abstract string Topic { get; }

    /// <summary>
    /// このカードが「重要」に該当するか。「今プレイできない」の判定は基底側で済ませてあるので、
    /// ここではカードの種類・状況だけを見ればよい。
    /// </summary>
    protected abstract bool Matches(PlayObserver observer, CardModel card);

    public override Utterance? Detect(PlayObserver observer, DetectorTrigger trigger)
    {
        CardModel? card = observer.LastDrawnCard;
        if (card == null || !IsStuckUnplayable(observer, card) || !Matches(observer, card))
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
            Probability = Probability,
            Tags = new[] { UtteranceTag.CardDrawn },
        };
    }

    /// <summary>
    /// 本当に「今ターン中はもうどうしようもない」か。エナジー不足**だけ**が理由なら、手札に今すぐプレイできる
    /// エナジー獲得カードが無い場合に限って「詰んでいる」とみなす(それがあれば、先に打てば実質プレイできるため)。
    /// エナジー以外の理由(対象不在・Unplayableキーワード等)が絡む場合は、エナジーを稼いでも解決しないので
    /// そのまま「詰んでいる」扱いにする。
    /// </summary>
    private static bool IsStuckUnplayable(PlayObserver observer, CardModel card)
    {
        if (card.CanPlay(out UnplayableReason reason, out _) || card.Keywords.Contains(CardKeyword.Sly))
        {
            return false;
        }

        if (card.Type is CardType.Attack && observer.HasPower<StampedePower>())
        {
            return false;
        }

        if ((reason & UnplayableReason.EnergyCostTooHigh) == 0)
        {
            return true;
        }

        return !observer.Hand.Any(c => c.CanPlay() && PlayObserver.EnergyCardProvide(c) + observer.Energy >= card.EnergyCost.GetResolved());
    }
}
