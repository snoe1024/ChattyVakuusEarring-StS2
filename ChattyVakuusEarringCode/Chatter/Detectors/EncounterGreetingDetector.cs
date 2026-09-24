using ChattyVakuusEarring.ChattyVakuusEarringCode.Chatter.Observer;
using ChattyVakuusEarring.ChattyVakuusEarringCode.Chatter.Speaker;
using System.Linq;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Localization;

namespace ChattyVakuusEarring.ChattyVakuusEarringCode.Chatter.Detectors;

/// <summary>
/// エリートやボスとの戦闘開始時に、相手の名前を挙げて言う。
/// </summary>
/// <remarks>
/// <para>
/// 初ターンはヴァクーが操作するのにまるで傍観してるみたいな台詞を言うのがやや微妙だったので一旦コメントアウト。
/// </para>
/// </remarks>
/**
 * jpn/relics.json
  "CHATTY-VAKUU-EARRING.ENCOUNTER_GREETING.elite.0": "[shake][red]おや、[/red][gold]{Enemy}[/gold][red]ですか。手強そうですねぇ。[/red][/shake]",
  "CHATTY-VAKUU-EARRING.ENCOUNTER_GREETING.elite.1": "[shake][gold]{Enemy}[/gold][red]…あなたに務まりますかねぇ？[/red][/shake]",
  "CHATTY-VAKUU-EARRING.ENCOUNTER_GREETING.boss.0": "[shake][red]ほう、[/red][gold]{Enemy}[/gold][red]…ここが正念場ですよ。[/red][/shake]",
  "CHATTY-VAKUU-EARRING.ENCOUNTER_GREETING.boss.1": "[shake][gold]{Enemy}[/gold][red]が相手とは…見物ですねぇ。[/red][/shake]"
 */
/**
 * eng/relics.json
  "CHATTY-VAKUU-EARRING.ENCOUNTER_GREETING.elite.0": "[shake][red]Oh, [/red][gold]{Enemy}[/gold][red]. Rather formidable, is it not?[/red][/shake]",
  "CHATTY-VAKUU-EARRING.ENCOUNTER_GREETING.elite.1": "[shake][gold]{Enemy}[/gold][red]... do you suppose you are equal to it?[/red][/shake]",
  "CHATTY-VAKUU-EARRING.ENCOUNTER_GREETING.boss.0": "[shake][red]Ah, [/red][gold]{Enemy}[/gold][red]... this is the moment that matters.[/red][/shake]",
  "CHATTY-VAKUU-EARRING.ENCOUNTER_GREETING.boss.1": "[shake][red]Facing [/red][gold]{Enemy}[/gold][red]... this should be worth watching.[/red][/shake]"
 */
/*
public sealed class EncounterGreetingDetector : PlayDetector
{
    private const string Topic = "ENCOUNTER_GREETING";

    private const int GreetingPriority = 5;

    public override DetectorTrigger Triggers => DetectorTrigger.VakuuTurnStarted;

    public override Utterance? Detect(PlayObserver observer, DetectorTrigger trigger)
    {
        string? kind = observer.EncounterRoomType switch
        {
            RoomType.Elite => "elite",
            RoomType.Boss => "boss",
            _ => null,
        };

        Creature? enemy = observer.LivingEnemies.FirstOrDefault(e => e.Monster != null);
        if (kind == null || enemy == null)
        {
            return null;
        }

        LocString? line = SpeechTable.Pick(Topic, kind);
        if (line == null)
        {
            return null;
        }

        line.Add("Enemy", enemy.Monster!.Title);
        return new Utterance
        {
            Line = line,
            Probability = 0.6f,
            Priority = GreetingPriority,
            Tags = new[] { UtteranceTag.Encounter },
        };
    }
}
*/