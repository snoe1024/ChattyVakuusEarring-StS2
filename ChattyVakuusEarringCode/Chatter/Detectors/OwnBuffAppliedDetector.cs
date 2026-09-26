using ChattyVakuusEarring.ChattyVakuusEarringCode.Chatter.Observer;
using ChattyVakuusEarring.ChattyVakuusEarringCode.Chatter.Speaker;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

namespace ChattyVakuusEarring.ChattyVakuusEarringCode.Chatter.Detectors;

/// <summary>
/// 持ち主自身が特定のバフ(<typeparamref name="TPower"/>)を獲得した時の低確率の一言。
/// 構造が同じ複数のバフ向けDetectorをまとめるジェネリックな中間基底クラス。
/// 具象クラスは<see cref="Topic"/>を返すだけで済む(まずは筋力・敏捷の2種。他のバフは必要に応じて追加)。
/// </summary>
/// <remarks>
/// 最初のターン(ヴァクーの代打ちとその余波)は<see cref="PlayObserver.IsFirstTurn"/>で除外する。
/// </remarks>
public abstract class OwnBuffAppliedDetector<TPower> : PlayDetector where TPower : PowerModel
{
    protected abstract string Topic { get; }

    private const float DefaultProbability = 0.15f;

    public override DetectorTrigger Triggers => DetectorTrigger.OwnBuffApplied;

    public override Utterance? Detect(PlayObserver observer, DetectorTrigger trigger)
    {
        // StrengthPower等はAllowNegative(自己減少)もあるので、増えた時だけに絞る。
        if (observer.IsFirstTurn || observer.LastBuffApplied is not { Power: TPower, Amount: > 0 })
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
            Tags = new[] { UtteranceTag.SelfBuff },
        };
    }
}

/// <summary>「筋力」(StrengthPower)を自身が獲得した時の一言。</summary>
public sealed class StrengthGainedDetector : OwnBuffAppliedDetector<StrengthPower>
{
    protected override string Topic => "STRENGTH_GAINED";
}

/// <summary>「敏捷」(DexterityPower)を自身が獲得した時の一言。</summary>
public sealed class DexterityGainedDetector : OwnBuffAppliedDetector<DexterityPower>
{
    protected override string Topic => "DEXTERITY_GAINED";
}
