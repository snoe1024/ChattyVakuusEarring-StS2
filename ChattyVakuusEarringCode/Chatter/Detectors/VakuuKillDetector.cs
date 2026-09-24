using ChattyVakuusEarring.ChattyVakuusEarringCode.Chatter.Observer;
using ChattyVakuusEarring.ChattyVakuusEarringCode.Chatter.Speaker;
using MegaCrit.Sts2.Core.Localization;

namespace ChattyVakuusEarring.ChattyVakuusEarringCode.Chatter.Detectors;

/// <summary>
/// 「私が倒してしまっても構わないのでしょう？」。ヴァクーが代打ちの最中に敵を倒した瞬間の一言。
/// </summary>
/// <remarks>
/// 倒した瞬間(<see cref="DetectorTrigger.EnemyKilled"/>)に判定する。喋るのは戦闘中に1回だけ
/// (確率判定で黙った場合も、2体目以降で言い直さない)。
/// 締めの講評(<see cref="FirstTurnReviewDetector"/>)は、この後に来る。
/// </remarks>
public sealed class VakuuKillDetector : PlayDetector
{
    private const string Topic = "VAKUU_KILL";

    private bool _proposed;

    public override DetectorTrigger Triggers => DetectorTrigger.EnemyKilled;

    public override Utterance? Detect(PlayObserver observer, DetectorTrigger trigger)
    {
        // 仲間(マルチプレイ)の代打ちで倒した場合は、こちらのヴァクーの手柄ではない。
        if (_proposed || !observer.IsInVakuuTurn || !observer.LastKillWasByOwner)
        {
            return null;
        }

        LocString? line = SpeechTable.Pick(Topic);
        if (line == null)
        {
            return null;
        }

        _proposed = true;
        return new Utterance
        {
            Line = line,
            Probability = 1f,
            Tags = new[] { UtteranceTag.Kill },
        };
    }
}
