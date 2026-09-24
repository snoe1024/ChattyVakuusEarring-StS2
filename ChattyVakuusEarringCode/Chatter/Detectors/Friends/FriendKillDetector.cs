using ChattyVakuusEarring.ChattyVakuusEarringCode.Chatter.Observer;
using ChattyVakuusEarring.ChattyVakuusEarringCode.Chatter.Speaker;
using MegaCrit.Sts2.Core.Localization;

namespace ChattyVakuusEarring.ChattyVakuusEarringCode.Chatter.Detectors.Friends;

/// <summary>
/// 「お友達の方が頑張っているようですよ。」。マルチプレイで、仲間が敵を倒した時の一言。
/// </summary>
/// <remarks>
/// 仲間のいない戦闘(ソロ)では発火しえないので、戦闘開始時に無効化される。
/// 倒したのが誰かは、ダメージを与えたクリーチャー(ペットならその持ち主)か、ダメージを与えたカードの持ち主から割り出している
/// (<see cref="KillAttribution"/>)。毒などの継続効果で倒れた場合は誰のものか分からないので対象外。
/// </remarks>
public sealed class FriendKillDetector : PlayDetector
{
    private const string Topic = "FRIEND_KILL";

    public override DetectorTrigger Triggers => DetectorTrigger.EnemyKilled;

    public override bool ShouldActivate(PlayObserver observer) => observer.HasOtherPlayers;

    public override Utterance? Detect(PlayObserver observer, DetectorTrigger trigger)
    {
        if (!observer.LastKillWasByOtherPlayer)
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
