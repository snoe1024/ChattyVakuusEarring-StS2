using ChattyVakuusEarring.ChattyVakuusEarringCode.Chatter.Observer;
using ChattyVakuusEarring.ChattyVakuusEarringCode.Chatter.Speaker;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Rooms;

namespace ChattyVakuusEarring.ChattyVakuusEarringCode.Chatter.Detectors;

/// <summary>
/// 戦闘に勝利した瞬間の一言(通常・エリート・ボスの三種)。
/// </summary>
public sealed class VictoryDetector : PlayDetector
{
    private const string Topic = "VICTORY";

    public override DetectorTrigger Triggers => DetectorTrigger.CombatWon;

    public override Utterance? Detect(PlayObserver observer, DetectorTrigger trigger)
    {
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
