using ChattyVakuusEarring.ChattyVakuusEarringCode.Chatter.Observer;
using ChattyVakuusEarring.ChattyVakuusEarringCode.Chatter.Speaker;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Rooms;

namespace ChattyVakuusEarring.ChattyVakuusEarringCode.Chatter.Detectors;

/// <summary>
/// 戦闘に勝利した瞬間の一言(通常・エリート・ボスの三種)。
/// </summary>
/// <remarks>
/// 「戦闘終了」そのものを表す<c>DetectorTrigger</c>は無い(<c>CombatEnded</c>イベントの時点では
/// <c>ChatterSession</c>自体が破棄処理に入っているため使えない)。そこで<see cref="DetectorTrigger.EnemyKilled"/>
/// (敵を倒した瞬間、誰が倒したかは問わず全セッションに届く)を使い、その撃破で
/// <see cref="PlayObserver.AreAllNonMinionEnemiesDefeated"/>(ミニオンを除いて誰も残っていない)が
/// trueになった時だけ「これが勝利を決めた一撃」とみなす。<see cref="VakuuKillDetector"/>が
/// 「ミニオンでない」かつ「最後の1体でない」撃破を対象にしているので、ちょうど補完関係になり両者は競合しない。
/// </remarks>
public sealed class VictoryDetector : PlayDetector
{
    private const string Topic = "VICTORY";

    public override DetectorTrigger Triggers => DetectorTrigger.EnemyKilled;

    public override Utterance? Detect(PlayObserver observer, DetectorTrigger trigger)
    {
        if (!observer.AreAllNonMinionEnemiesDefeated)
        {
            return null;
        }

        (string Kind, float Probability)? info = observer.EncounterRoomType switch
        {
            RoomType.Monster => ("monster", 0.35f),
            RoomType.Elite => ("elite", 0.55f),
            RoomType.Boss => ("boss", 0.7f),
            _ => null,
        };

        if (info == null)
        {
            return null;
        }

        LocString? line = SpeechTable.Pick(Topic, info.Value.Kind);
        if (line == null)
        {
            return null;
        }

        return new Utterance
        {
            Line = line,
            Probability = info.Value.Probability,
            Tags = new[] { UtteranceTag.Kill },
        };
    }
}
