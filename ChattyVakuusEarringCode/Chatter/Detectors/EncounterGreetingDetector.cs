using System.Linq;
using ChattyVakuusEarring.ChattyVakuusEarringCode.Chatter.Observer;
using ChattyVakuusEarring.ChattyVakuusEarringCode.Chatter.Speaker;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Rooms;

namespace ChattyVakuusEarring.ChattyVakuusEarringCode.Chatter.Detectors;

/// <summary>
/// 戦闘開始時に、相手の名前を挙げて一言(通常・エリート・ボスの三種)。
/// </summary>
/// <remarks>
/// 元々は<c>VakuuTurnStarted</c>(手札が配られ、ヴァクーの代打ちが始まる直前)で発火させていたが、
/// ヴァクー自身が操作するターンなのに傍観者のような台詞になって微妙だったため、一旦コメントアウトしていた。
/// 囁きのイヤリングを所持していなくても囁く機能が公式になったため再有効化した。通常戦は頻度が高いので確率を抑えてある。
/// </remarks>
public sealed class EncounterGreetingDetector : PlayDetector
{
    private const string Topic = "ENCOUNTER_GREETING";

    private const int GreetingPriority = 5;

    public override DetectorTrigger Triggers => DetectorTrigger.PlayerTurnStarted;

    public override bool ShouldActivate(PlayObserver observer) => observer.HasRelic<WhisperingEarring>();

    public override Utterance? Detect(PlayObserver observer, DetectorTrigger trigger)
    {
        if (!observer.IsFirstTurn)
        {
            return null;
        }

        (string Kind, float Probability)? info = observer.EncounterRoomType switch
        {
            RoomType.Monster => ("monster", 0.25f),
            RoomType.Elite => ("elite", 0.6f),
            RoomType.Boss => ("boss", 0.65f),
            _ => null,
        };

        Creature? enemy = observer.LivingEnemies.FirstOrDefault(e => !e.HasPower<MinionPower>() && e.Monster != null);
        if (info == null || enemy == null)
        {
            return null;
        }

        LocString? line = SpeechTable.Pick(Topic, info.Value.Kind);
        if (line == null)
        {
            return null;
        }

        line.Add("Enemy", enemy.Monster!.Title);
        return new Utterance
        {
            Line = line,
            Probability = info.Value.Probability,
            Priority = GreetingPriority,
            Tags = new[] { UtteranceTag.Encounter },
        };
    }
}
