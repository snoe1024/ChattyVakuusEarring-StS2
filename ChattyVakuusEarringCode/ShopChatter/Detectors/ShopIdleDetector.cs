using ChattyVakuusEarring.ChattyVakuusEarringCode.Chatter.Speaker;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models.Relics;

namespace ChattyVakuusEarring.ChattyVakuusEarringCode.ShopChatter.Detectors;

public class ShopIdleDetector : ShopDetector
{
    private const string Topic = "SHOP_IDLE";

    /// <summary>この放置時間(秒)を超えてから、毎Tickの発言判定を始める。</summary>
    private const double IdleThresholdSeconds = 15;

    /// <summary>閾値を超えた後の、Tick(約1秒)ごとの発言確率。</summary>
    private const float SpeakProbabilityPerTick = 0.03f;
    
    public override ShopDetectorTrigger Triggers => ShopDetectorTrigger.Tick;

    public override Utterance? Detect(ShopObserver observer, ShopDetectorTrigger trigger)
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

        return new Utterance()
        {
            Line = line,
            Probability = SpeakProbabilityPerTick,
            Tags = new[] { UtteranceTag.Idle, UtteranceTag.Shop },
        };
    }
}