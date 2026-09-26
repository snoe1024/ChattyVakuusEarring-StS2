using ChattyVakuusEarring.ChattyVakuusEarringCode.Chatter.Observer;
using ChattyVakuusEarring.ChattyVakuusEarringCode.Chatter.Speaker;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

namespace ChattyVakuusEarring.ChattyVakuusEarringCode.Chatter.Detectors;

/// <summary>
/// 持ち主が敵に特定のデバフ(<typeparamref name="TPower"/>)を付与した時の一言。
/// 構造が同じ複数のデバフ向けDetector(弱体・脱力・毒・衰弱・破滅)をまとめるジェネリックな中間基底クラス。
/// 具象クラスは<see cref="Topic"/>を返すだけで済む。
/// </summary>
/// <remarks>
/// 最初のターン(ヴァクーの代打ちとその余波)は<see cref="PlayObserver.IsFirstTurn"/>で除外する
/// (プレイヤー自身の判断とは言えないため)。
/// </remarks>
public abstract class DebuffAppliedDetector<TPower> : PlayDetector where TPower : PowerModel
{
    protected abstract string Topic { get; }

    private const float DefaultProbability = 0.25f;

    public override DetectorTrigger Triggers => DetectorTrigger.DebuffApplied;

    public override Utterance? Detect(PlayObserver observer, DetectorTrigger trigger)
    {
        if (observer.IsFirstTurn || observer.LastDebuffApplied?.Power is not TPower)
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
            Probability = DefaultProbability,
            Tags = new[] { UtteranceTag.Attacking },
        };
    }
}

/// <summary>「弱体」(VulnerablePower、被ダメージ増加)を敵に付与した時の一言。</summary>
public sealed class VulnerableAppliedDetector : DebuffAppliedDetector<VulnerablePower>
{
    protected override string Topic => "VULNERABLE_APPLIED";
}

/// <summary>「脱力」(WeakPower、攻撃力減少)を敵に付与した時の一言。</summary>
public sealed class WeakAppliedDetector : DebuffAppliedDetector<WeakPower>
{
    protected override string Topic => "WEAK_APPLIED";
}

/// <summary>「毒」(PoisonPower)を敵に付与した時の一言。</summary>
public sealed class PoisonAppliedDetector : DebuffAppliedDetector<PoisonPower>
{
    protected override string Topic => "POISON_APPLIED";
}

/// <summary>「衰弱」(DebilitatePower)を敵に付与した時の一言。</summary>
public sealed class DebilitateAppliedDetector : DebuffAppliedDetector<DebilitatePower>
{
    protected override string Topic => "DEBILITATE_APPLIED";
}

/// <summary>「破滅」(DoomPower)を敵に付与した時の一言。</summary>
public sealed class DoomAppliedDetector : DebuffAppliedDetector<DoomPower>
{
    protected override string Topic => "DOOM_APPLIED";
}
