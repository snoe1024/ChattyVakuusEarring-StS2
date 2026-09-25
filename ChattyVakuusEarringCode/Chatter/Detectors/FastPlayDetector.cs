using System.Diagnostics;
using ChattyVakuusEarring.ChattyVakuusEarringCode.Chatter.Observer;
using ChattyVakuusEarring.ChattyVakuusEarringCode.Chatter.Speaker;
using MegaCrit.Sts2.Core.Localization;

namespace ChattyVakuusEarring.ChattyVakuusEarringCode.Chatter.Detectors;

/// <summary>
/// 手札を吟味せず、カードを異常な速さで連続プレイし続けている時の一言(中確率)。1戦闘に1度。
/// </summary>
/// <remarks>
/// <para>
/// ラップタイムは<see cref="DetectorTrigger.CardPlayed"/>(演出込みの<c>CardPlayFinishedEntry</c>)ではなく
/// <see cref="DetectorTrigger.CardPlayStarted"/>(<c>CardPlayStartedEntry</c>)で計測する
/// (演出時間を挟むFinishedでは、実際にボタンを押した間隔を直接測れない)。
/// </para>
/// <para>
/// 計測区間はターンごとに「操作可能になった時」(<see cref="DetectorTrigger.PlayerTurnStarted"/>、
/// 最初のターンなら代打ちが終わった後)から「ターン終了を押した時」(<see cref="DetectorTrigger.PlayerEndedTurn"/>)
/// まで。ターン開始のたびに基準時刻を取り直すことで、区間の外側(敵のターンの処理時間等)を平均に混ぜない。
/// 自動プレイ(ヴァクーの代打ち等)は対象外。
/// </para>
/// </remarks>
public sealed class FastPlayDetector : PlayDetector
{
    private const string Topic = "FAST_PLAY";

    /// <summary>これ以上の枚数を手動プレイしていたら判定対象にする。</summary>
    private const int MinCards = 20;

    /// <summary>プレイ間隔の平均がこれ以下なら「早指し」とみなす。</summary>
    private const double MaxAverageIntervalSeconds = 1.0;

    private long? _referenceTimestamp;

    private int _manualPlayCount;

    private double _totalIntervalSeconds;

    private bool _fired;

    public override DetectorTrigger Triggers =>
        DetectorTrigger.PlayerTurnStarted | DetectorTrigger.PlayerEndedTurn | DetectorTrigger.CardPlayStarted;

    public override Utterance? Detect(PlayObserver observer, DetectorTrigger trigger)
    {
        if (_fired)
        {
            return null;
        }

        long now = Stopwatch.GetTimestamp();

        switch (trigger)
        {
            case DetectorTrigger.PlayerTurnStarted:
                _referenceTimestamp = now;
                return null;
            case DetectorTrigger.PlayerEndedTurn:
                _referenceTimestamp = null;
                return null;
        }

        // CardPlayStarted。ヴァクーの代打ち等の自動プレイはプレイヤーの早指しではないので対象外。
        if (_referenceTimestamp == null || observer.LastCardPlayStarted is not { IsAutoPlay: false })
        {
            return null;
        }

        _totalIntervalSeconds += Stopwatch.GetElapsedTime(_referenceTimestamp.Value, now).TotalSeconds;
        _manualPlayCount++;
        _referenceTimestamp = now;

        if (_manualPlayCount < MinCards || _totalIntervalSeconds / _manualPlayCount > MaxAverageIntervalSeconds)
        {
            return null;
        }

        LocString? line = SpeechTable.Pick(Topic);
        if (line == null)
        {
            return null;
        }

        _fired = true;
        return new Utterance
        {
            Line = line,
            Probability = 0.4f,
            Tags = new[] { UtteranceTag.Pace },
        };
    }
}
