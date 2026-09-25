using System.Linq;
using ChattyVakuusEarring.ChattyVakuusEarringCode.Chatter.Observer;
using ChattyVakuusEarring.ChattyVakuusEarringCode.Chatter.Speaker;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Saves;

namespace ChattyVakuusEarring.ChattyVakuusEarringCode.Chatter.Detectors;

/// <summary>
/// エリート・ボス戦の開始時、その相手に対する勝率が極端に高い・低い時の一言。
/// <see cref="EncounterGreetingDetector"/>より優先度を高くしてあり、該当する時はそちらの
/// 名前だけの一言より先に判断される(こちらが確率判定で黙れば、通常通りそちらに順番が回る)。
/// </summary>
/// <remarks>
/// <para>
/// 勝敗記録は、本家がモンスター図鑑向けに戦闘終了のたびに増分更新している
/// <c>SaveManager.Instance.Progress.EncounterStats</c>(<see cref="PlayObserver.GetEncounterStats"/>)を
/// そのまま使う。プレイ履歴を毎回全走査するような重い実装ではなく、辞書引きのみで済む。
/// エリート・ボスは複数体で構成されうる戦闘があるため、個々のモンスター単位の<c>EnemyStats</c>ではなく、
/// 戦闘全体を表す<c>EncounterStats</c>(モンスター図鑑がエリート・ボスのエントリを個々のモンスターではなく
/// エンカウント単位で表示するのと同じ単位)を使う。
/// </para>
/// <para>
/// サンプル数が少ないと数字が安定しないので、現在のキャラクターでの対戦数が<see cref="MinCharacterFights"/>
/// 以上あればそのキャラクター限定の勝率を使い、足りなければ全キャラクター合算の対戦数が
/// <see cref="MinTotalFights"/>以上あればそちらを使う。どちらも満たさなければ判定しない。
/// </para>
/// </remarks>
public sealed class EncounterWinRateGreetingDetector : PlayDetector
{
    private const string Topic = "ENCOUNTER_WIN_RATE_GREETING";

    private const int WinRatePriority = 15;

    private const int MinCharacterFights = 5;

    private const int MinTotalFights = 10;

    private const double EliteHighThreshold = 0.98;

    private const double EliteLowThreshold = 0.90;

    private const double BossHighThreshold = 0.95;

    private const double BossLowThreshold = 0.80;

    public override DetectorTrigger Triggers => DetectorTrigger.PlayerTurnStarted;

    public override bool ShouldActivate(PlayObserver observer) => !observer.HasRelic<WhisperingEarring>();

    public override Utterance? Detect(PlayObserver observer, DetectorTrigger trigger)
    {
        if (!observer.IsFirstTurn)
        {
            return null;
        }

        (string RoomKind, double High, double Low)? info = observer.EncounterRoomType switch
        {
            RoomType.Elite => ("elite", EliteHighThreshold, EliteLowThreshold),
            RoomType.Boss => ("boss", BossHighThreshold, BossLowThreshold),
            _ => null,
        };

        if (info == null)
        {
            return null;
        }

        EncounterStats? stats = observer.GetEncounterStats();
        if (stats == null)
        {
            return null;
        }

        FightStats? characterFight = stats.FightStats.FirstOrDefault(f => f.Character == observer.Owner.Character.Id);
        int characterFights = (characterFight?.Wins ?? 0) + (characterFight?.Losses ?? 0);

        double winRate;
        if (characterFights >= MinCharacterFights)
        {
            winRate = (double)characterFight!.Wins / characterFights;
        }
        else
        {
            int totalFights = stats.TotalWins + stats.TotalLosses;
            if (totalFights < MinTotalFights)
            {
                return null;
            }

            winRate = (double)stats.TotalWins / totalFights;
        }

        string? tier = winRate >= info.Value.High ? "high" : winRate <= info.Value.Low ? "low" : null;
        if (tier == null)
        {
            return null;
        }

        LocString? line = SpeechTable.Pick(Topic, $"{info.Value.RoomKind}_{tier}");
        if (line == null)
        {
            return null;
        }

        line.Add("Enemy", observer.Encounter!.Title);
        return new Utterance
        {
            Line = line,
            Probability = 0.5f,
            Priority = WinRatePriority,
            Tags = new[] { UtteranceTag.Encounter },
        };
    }
}
