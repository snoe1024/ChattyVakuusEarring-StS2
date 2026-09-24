using System.Collections.Generic;
using System.Linq;
using ChattyVakuusEarring.ChattyVakuusEarringCode.Chatter.Observer;
using ChattyVakuusEarring.ChattyVakuusEarringCode.Chatter.Speaker;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models.Relics;

namespace ChattyVakuusEarring.ChattyVakuusEarringCode.Chatter.Detectors.Relics;

/// <summary>
/// 「暦石が泣いてますよ」。暦石が発動するターン(既定7ターン目)、そのターンが始まった時点で既に
/// 暦石のダメージ(既定52)だけで倒せていたはずの敵に、わざわざカードでダメージを与えていたら言う。
/// </summary>
/// <remarks>
/// <para>
/// 暦石は「発動ターンの終了時、敵全体に固定ダメージ」を与えるレリックなので、そのターンの開始時点で
/// 既にHP+ブロック合計がそのダメージ以下だった敵は、プレイヤーが何もしなくても暦石が始末してくれたはず。
/// それに構わずカードでダメージを与えていたのなら、その分は無駄だった、とみなす。
/// </para>
/// <para>
/// 判定はターン開始時点のスナップショットが必要なので、<see cref="DetectorTrigger.PlayerTurnStarted"/>で
/// 「既に詰んでいた敵」を記録しておき、<see cref="DetectorTrigger.PlayerEndedTurn"/>(暦石自身がダメージを
/// 与えるより前)で、そのターン中にその敵へカードでダメージを与えていたかを確認する
/// (敵のHP/ブロックはターン中の行動で変わるため、ターン終了時点の値を見てはいけない)。
/// </para>
/// <para>
/// 暦石のダメージはUnpowered(脆弱・弱体の影響を受けない)固定値なので、HP+ブロックの単純な合計と比較するだけでよい
/// (「数値通り」に比較してよい、という設計メモの通り)。発動ターン・ダメージ量は暦石自身の<c>DynamicVars</c>から読む。
/// このレリックは発動条件・タイミングが全て公開状態から追えるので、
/// <see cref="WastedBlockRelicComplaintDetector"/>と違いHarmonyパッチは不要。
/// </para>
/// </remarks>
public sealed class StoneCalendarComplaintDetector : PlayDetector
{
    private const string Topic = "STONE_CALENDAR_COMPLAINT";

    /// <summary>暦石が発動するターンの開始時点で、既に暦石のダメージだけで倒せていたはずの敵。</summary>
    private HashSet<Creature>? _alreadyDoomedEnemies;

    public override DetectorTrigger Triggers => DetectorTrigger.PlayerTurnStarted | DetectorTrigger.PlayerEndedTurn;

    public override bool ShouldActivate(PlayObserver observer) => observer.HasRelic<StoneCalendar>();

    public override Utterance? Detect(PlayObserver observer, DetectorTrigger trigger)
    {
        StoneCalendar relic = observer.GetRelic<StoneCalendar>()!;
        if (observer.TurnNumber != relic.DynamicVars["DamageTurn"].IntValue)
        {
            return null;
        }

        int damageThreshold = relic.DynamicVars.Damage.IntValue;

        if (trigger == DetectorTrigger.PlayerTurnStarted)
        {
            _alreadyDoomedEnemies = observer.LivingEnemies
                .Where(enemy => enemy.CurrentHp + enemy.Block <= damageThreshold)
                .ToHashSet();
            return null;
        }

        if (_alreadyDoomedEnemies is not { Count: > 0 })
        {
            return null;
        }

        bool attackedAnAlreadyDoomedEnemy = observer.EnemiesDamagedByCardsThisTurn()
            .Any(_alreadyDoomedEnemies.Contains);
        if (!attackedAnAlreadyDoomedEnemy)
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
            Probability = 0.8f,
            Tags = new[] { UtteranceTag.RelicComplaint, UtteranceTag.Attacking },
        };
    }
}
