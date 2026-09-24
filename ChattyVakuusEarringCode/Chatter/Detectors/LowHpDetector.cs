using ChattyVakuusEarring.ChattyVakuusEarringCode.Chatter.Observer;
using ChattyVakuusEarring.ChattyVakuusEarringCode.Chatter.Speaker;
using MegaCrit.Sts2.Core.Localization;

namespace ChattyVakuusEarring.ChattyVakuusEarringCode.Chatter.Detectors;

/// <summary>
/// 「そろそろ危ういのではありませんか？」。ダメージを受けてHPが危険域まで減った瞬間の一言。
/// </summary>
/// <remarks>
/// 危険域に入るたびに1度だけ言う(危険域に居続けても、ダメージを受けるたびには言わない)。
/// 回復して<see cref="RearmHpRatio"/>を超えたら、また言えるようになる。
/// 同じダメージで<see cref="HeavyHitDetector"/>の一言も出そうな場面では、こちらを優先する
/// (こちらが確率判定で黙った場合は、そちらに順番が回る)。
/// </remarks>
public sealed class LowHpDetector : PlayDetector
{
    private const string Topic = "LOW_HP";

    /// <summary>最大HPに対するこの割合以下(かつ生存中)を危険域とする。</summary>
    private const double LowHpRatio = 0.3;

    /// <summary>危険域で一度言った後、HPがこの割合を超えるまで回復したら再び言えるようにする。</summary>
    private const double RearmHpRatio = 0.5;

    private const int LowHpPriority = 10;

    private bool _armed = true;

    public override DetectorTrigger Triggers => DetectorTrigger.DamageTaken;

    public override Utterance? Detect(PlayObserver observer, DetectorTrigger trigger)
    {
        if (observer.Hp > observer.MaxHp * RearmHpRatio)
        {
            _armed = true;
        }

        if (!_armed || observer.Hp <= 0 || observer.Hp > observer.MaxHp * LowHpRatio)
        {
            return null;
        }

        LocString? line = SpeechTable.Pick(Topic);
        if (line == null)
        {
            return null;
        }

        _armed = false;
        return new Utterance
        {
            Line = line,
            Probability = 0.9f,
            Priority = LowHpPriority,
            Tags = new[] { UtteranceTag.Danger },
        };
    }
}
