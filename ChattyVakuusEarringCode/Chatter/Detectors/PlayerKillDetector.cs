using ChattyVakuusEarring.ChattyVakuusEarringCode.Chatter.Observer;
using ChattyVakuusEarring.ChattyVakuusEarringCode.Chatter.Speaker;
using MegaCrit.Sts2.Core.Localization;

namespace ChattyVakuusEarring.ChattyVakuusEarringCode.Chatter.Detectors;

/// <summary>
/// 「その調子ですよ。」。プレイヤー自身が敵を倒した時の一言。
/// </summary>
/// <remarks>
/// 敵を倒すたびに判定される頻度の高い出来事なので、確率は低めにしてある。
/// ヴァクーの代打ち中の撃破は<see cref="VakuuKillDetector"/>の担当なので、こちらでは扱わない。
/// </remarks>
public sealed class PlayerKillDetector : PlayDetector
{
    private const string Topic = "PLAYER_KILL";

    public override DetectorTrigger Triggers => DetectorTrigger.EnemyKilled;

    public override Utterance? Detect(PlayObserver observer, DetectorTrigger trigger)
    {
        if (!observer.LastKillWasByOwner || observer.IsInVakuuTurn)
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
            Probability = 0.5f,
            Tags = new[] { UtteranceTag.Kill },
        };
    }
}
