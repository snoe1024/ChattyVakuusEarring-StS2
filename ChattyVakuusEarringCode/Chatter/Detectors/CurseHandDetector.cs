using System.Linq;
using ChattyVakuusEarring.ChattyVakuusEarringCode.Chatter.Observer;
using ChattyVakuusEarring.ChattyVakuusEarringCode.Chatter.Speaker;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Localization;

namespace ChattyVakuusEarring.ChattyVakuusEarringCode.Chatter.Detectors;

/// <summary>
/// 「呪いにまみれた姿…お似合いですよ。」。配られた手札の3分の1以上が呪いだと言う。
/// </summary>
/// <remarks>
/// <see cref="HandQualityDetector"/>と同じタイミング(初期手札が配られた時)で判定する。呪いは評価値が低いので、
/// 呪いだらけの手札は<see cref="HandQualityDetector"/>の「酷い手札」にも該当しがちだが、
/// こちらの方が言いたいことが具体的なので、<see cref="Utterance.Priority"/>を高くして先に判断させている。
/// こちらが喋れば「酷い手札」は間隔ルールで黙り、こちらが確率判定で黙った場合は「酷い手札」に順番が回る。
/// </remarks>
public sealed class CurseHandDetector : PlayDetector
{
    private const string Topic = "CURSE_HAND";

    /// <summary>手札のうち、呪いがこの割合以上ならこの台詞の対象とする。</summary>
    private const double CurseHandRatio = 1.0 / 3.0;

    /// <summary><see cref="HandQualityDetector"/>(既定の0)より優先させるための値。</summary>
    private const int CursePriority = 10;

    public override DetectorTrigger Triggers =>
        DetectorTrigger.VakuuTurnStarted | DetectorTrigger.PlayerTurnStarted;
    
    public override bool ShouldActivate(PlayObserver observer) =>
        observer.Deck.Count(c => c.Rarity is CardRarity.Curse) >= 2;

    public override Utterance? Detect(PlayObserver observer, DetectorTrigger trigger)
    {
        if (!TryStartEvaluationForTurn(observer) || observer.Hand.Count == 0)
        {
            return null;
        }

        int curses = observer.Hand.Count(card => card.Rarity == CardRarity.Curse);
        if (curses == 0 || curses < observer.Hand.Count * CurseHandRatio)
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
            Probability = 0.9f,
            Priority = CursePriority,
            Tags = new[] { UtteranceTag.HandQuality },
        };
    }
}
