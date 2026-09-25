using ChattyVakuusEarring.ChattyVakuusEarringCode.Chatter.Observer;
using ChattyVakuusEarring.ChattyVakuusEarringCode.Chatter.Speaker;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Localization;

namespace ChattyVakuusEarring.ChattyVakuusEarringCode.Chatter.Detectors;

/// <summary>
/// 3ターン以上連続して敵からダメージを受け続けている時の一言(高確率)。
/// </summary>
/// <remarks>
/// <c>CombatHistoryEntry</c>は各エントリが何ターン目に起きたかを外部に公開していないため、過去の履歴から
/// 連続ターン数を遡って再計算することはできない。そこで<see cref="DetectorTrigger.DamageTaken"/>で
/// 「直前のサイクルで被弾したか」を溜め込み、次の<see cref="DetectorTrigger.PlayerTurnStarted"/>
/// (このサイクル=前のターン+それに続く敵のターンの終わり)で連続ターン数を更新する。
/// </remarks>
public sealed class ConsecutiveDamageDetector : PlayDetector
{
    private const string Topic = "CONSECUTIVE_DAMAGE";

    /// <summary>これ以上連続で被弾していたら言う。</summary>
    private const int RequiredStreak = 3;

    private bool _damagedSinceLastCheck;

    private int _streak;

    public override DetectorTrigger Triggers => DetectorTrigger.DamageTaken | DetectorTrigger.PlayerTurnStarted;

    public override Utterance? Detect(PlayObserver observer, DetectorTrigger trigger)
    {
        if (trigger == DetectorTrigger.DamageTaken)
        {
            var taken = observer.LastDamageTaken;
            if (taken?.Dealer?.Side == CombatSide.Enemy && taken.Result.UnblockedDamage > 0)
            {
                _damagedSinceLastCheck = true;
            }

            return null;
        }

        // PlayerTurnStarted。最初のターンには「直前のサイクル」が無いので、集計だけリセットして判定はしない。
        if (observer.IsFirstTurn)
        {
            _damagedSinceLastCheck = false;
            _streak = 0;
            return null;
        }

        _streak = _damagedSinceLastCheck ? _streak + 1 : 0;
        _damagedSinceLastCheck = false;

        if (_streak < RequiredStreak)
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
            Probability = 0.75f,
            Tags = new[] { UtteranceTag.Danger },
        };
    }
}
