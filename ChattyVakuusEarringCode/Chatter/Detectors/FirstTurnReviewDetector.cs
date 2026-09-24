using System.Collections.Generic;
using System.Linq;
using ChattyVakuusEarring.ChattyVakuusEarringCode.Chatter.Observer;
using ChattyVakuusEarring.ChattyVakuusEarringCode.Chatter.Speaker;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Relics;

namespace ChattyVakuusEarring.ChattyVakuusEarringCode.Chatter.Detectors;

/// <summary>
/// 最初のターンにヴァクーが代打ちした結果への講評。バニラの「なかなか上手いものでしょう？」(approval)と
/// 「もういいでしょう。」(warning)をこのmod側に引き取ったもの。
/// </summary>
/// <remarks>
/// バニラと同じく必ず喋る(<see cref="Utterance.Force"/>)が、内容は代打ちの結果次第で変える。
/// ただしヴァクーが代打ち中に敵を倒していた場合は、<see cref="VakuuKillDetector"/>の一言との兼ね合いで
/// 強制せず、直前に喋っていたらスキップする(時間が経っていれば喋る)。
/// <list type="bullet">
/// <item><c>warning</c> — 代打ちの上限(13枚)まで打った。</item>
/// <item><c>approval</c> — それ以外。</item>
/// </list>
/// 「配られた手札そのものが酷い」という評価は代打ちの結果とは別物なので、<see cref="HandQualityDetector"/>が担当する。
/// バニラは代打ちで1枚も打てなかった時は無言なので、その場合は<see cref="DetectorTrigger.VakuuFinishedPlaying"/>自体が来ない。
/// 各種別の閾値は初期のヒューリスティックなので、実際に遊んで違和感があれば調整する。
/// </remarks>
public sealed class FirstTurnReviewDetector : PlayDetector
{
    private const string Topic = "FIRST_TURN_END";

    /// <summary>これ以上のコストのカードを「高コスト」とみなす。</summary>
    private const int HighCostThreshold = 2;

    public override DetectorTrigger Triggers => DetectorTrigger.VakuuFinishedPlaying;

    public override Utterance? Detect(PlayObserver observer, DetectorTrigger trigger)
    {
        string kind;
        LocString fallback;
        var tags = new List<UtteranceTag> { UtteranceTag.FirstTurnReview };

        if (observer.AutoPlayedCardCountThisTurn >= WhisperingEarring.maxCardsToPlay)
        {
            kind = "warning";
            fallback = VanillaEarringLines.Warning();
        }
        else
        {
            kind = "approval";
            fallback = VanillaEarringLines.Approval();
        }

        LocString? line = SpeechTable.Pick(Topic, kind, fallback);
        if (line == null)
        {
            return null;
        }

        // ヴァクーが代打ち中に敵を倒していたら、倒した時に既に何か言っている(VakuuKillDetector)ので、
        // 締めの一言は強制せず、Speakerの間隔ルールに任せる。直前に喋っていればスキップ、時間が経っていれば喋る。
        bool killedDuringVakuuTurn = observer.EnemiesKilledThisTurn().Any();
        return new Utterance { Line = line, Force = !killedDuringVakuuTurn, Tags = tags };
    }
}
