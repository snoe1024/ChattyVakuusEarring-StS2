using ChattyVakuusEarring.ChattyVakuusEarringCode.Chatter.Observer;
using ChattyVakuusEarring.ChattyVakuusEarringCode.Chatter.Speaker;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Rooms;

namespace ChattyVakuusEarring.ChattyVakuusEarringCode.Chatter.Detectors;

/// <summary>
/// 一度も実HPダメージを受けずに戦闘に勝利した(完封勝利)瞬間の一言(通常・エリート・ボスの三種)。
/// </summary>
/// <remarks>
/// <see cref="VictoryDetector"/>より優先度を高くしてあり、完封勝利の場面ではこちらが先に判断される
/// (確率判定で黙れば通常通り<see cref="VictoryDetector"/>に順番が回る)。
/// </remarks>
public sealed class FlawlessVictoryDetector : PlayDetector
{
    private const string Topic = "FLAWLESS_VICTORY";

    private const int FlawlessPriority = 10;

    public override DetectorTrigger Triggers => DetectorTrigger.EnemyKilled;

    public override Utterance? Detect(PlayObserver observer, DetectorTrigger trigger)
    {
        if (!observer.AreAllNonMinionEnemiesDefeated || observer.HasTakenUnblockedDamageThisCombat())
        {
            return null;
        }

        (string Kind, float Probability)? info = observer.EncounterRoomType switch
        {
            RoomType.Monster => ("monster", 0.5f),
            RoomType.Elite => ("elite", 0.75f),
            RoomType.Boss => ("boss", 0.9f),
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
            Priority = FlawlessPriority,
            Tags = new[] { UtteranceTag.Kill },
        };
    }
}
