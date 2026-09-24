using System;
using ChattyVakuusEarring.ChattyVakuusEarringCode.Chatter.Observer;
using ChattyVakuusEarring.ChattyVakuusEarringCode.Chatter.Speaker;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Localization;

namespace ChattyVakuusEarring.ChattyVakuusEarringCode.Chatter.Detectors;

/// <summary>
/// 「今のは痛そうでしたねぇ。」。敵から大きなダメージ(HPが実際に減った量)を受けた瞬間の一言。
/// </summary>
/// <remarks>
/// 「大きい」の基準は、最大HPの<see cref="HeavyHitMaxHpRatio"/>以上(ただし最低<see cref="HeavyHitMinDamage"/>)。
/// 自分のカードによる自傷や、毒などの継続効果(ダメージを与えた側が敵でないもの)は対象外。
/// </remarks>
public sealed class HeavyHitDetector : PlayDetector
{
    private const string Topic = "HEAVY_HIT";

    /// <summary>最大HPに対するこの割合以上のHP減少を「大ダメージ」とする。</summary>
    private const double HeavyHitMaxHpRatio = 0.2;

    /// <summary>最大HPが小さくても、これ未満のHP減少は「大ダメージ」とはみなさない。</summary>
    private const int HeavyHitMinDamage = 8;

    public override DetectorTrigger Triggers => DetectorTrigger.DamageTaken;

    public override Utterance? Detect(PlayObserver observer, DetectorTrigger trigger)
    {
        var taken = observer.LastDamageTaken;
        if (taken?.Dealer?.Side != CombatSide.Enemy)
        {
            return null;
        }

        double threshold = Math.Max(HeavyHitMinDamage, observer.MaxHp * HeavyHitMaxHpRatio);
        if (taken.Result.UnblockedDamage < threshold)
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
            Probability = 0.6f,
            Tags = new[] { UtteranceTag.Danger },
        };
    }
}
