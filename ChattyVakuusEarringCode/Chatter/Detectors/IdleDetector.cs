using ChattyVakuusEarring.ChattyVakuusEarringCode.Chatter.Observer;
using ChattyVakuusEarring.ChattyVakuusEarringCode.Chatter.Speaker;
using MegaCrit.Sts2.Core.Localization;

namespace ChattyVakuusEarring.ChattyVakuusEarringCode.Chatter.Detectors;

/// <summary>
/// 「手が止まっていますよ。」。プレイヤーが自分のターンで何も操作しないまま時間が経った時の放置テキスト。
/// </summary>
/// <remarks>
/// <para>
/// 時間ベースのトリガー(<see cref="DetectorTrigger.Tick"/>)の実例。<see cref="PlayObserver.IdleSeconds"/>が
/// <see cref="IdleThresholdSeconds"/>を超えてからは、Tick(約1秒)のたびに低い確率(<see cref="SpeakProbabilityPerTick"/>)で
/// 発言を提案する。決まった間隔ではなく毎回の確率判定なので、発言のタイミングがランダムに見える
/// (0.02なら、閾値を超えてから平均して+約25秒後、半分は+約10秒以内に言う計算)。
/// </para>
/// <para>
/// 操作(カードのプレイ・ポーション使用)や、フェーズ・ターンの切り替えで放置時間は0に戻るので、閾値からの数え直しになる。
/// 発言の判定は自分の端末のプレイヤーに対してだけ行う(他のプレイヤーの放置は、その人の端末が見ている)。
/// </para>
/// </remarks>
public sealed class IdleDetector : PlayDetector
{
    private const string Topic = "IDLE";

    /// <summary>この放置時間(秒)を超えてから、毎Tickの発言判定を始める。</summary>
    private const double IdleThresholdSeconds = 20;

    /// <summary>閾値を超えた後の、Tick(約1秒)ごとの発言確率。</summary>
    private const float SpeakProbabilityPerTick = 0.02f;

    public override DetectorTrigger Triggers => DetectorTrigger.Tick;

    public override Utterance? Detect(PlayObserver observer, DetectorTrigger trigger)
    {
        if (observer.IdleSeconds < IdleThresholdSeconds)
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
            Probability = SpeakProbabilityPerTick,
            Tags = new[] { UtteranceTag.Idle },
        };
    }
}
