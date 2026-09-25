using ChattyVakuusEarring.ChattyVakuusEarringCode.Chatter.Speaker;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models.Relics;

namespace ChattyVakuusEarring.ChattyVakuusEarringCode.ShopChatter.Detectors;

/// <summary>
/// 買い物をして、所持ゴールドが25以下まで減った状態でショップ画面を閉じた時の一言。
/// 汎用の<see cref="ShopClosedDetector"/>と同じ場面で出うるが、優先度を上げてこちらを先に判断させる。
/// </summary>
public class ShopSpentDownDetector : ShopDetector
{
    /// <summary>これ以下まで使い切ったら「ほぼ使い切った」とみなす。</summary>
    private const int LowGoldThreshold = 25;

    private const string Topic = "SHOP_CLOSED_SPENT_DOWN";

    public override ShopDetectorTrigger Triggers => ShopDetectorTrigger.ShopClosed;

    public override bool ShouldActivate(ShopObserver observer) => !observer.HasRelic<LordsParasol>();

    public override Utterance? Detect(ShopObserver observer, ShopDetectorTrigger trigger)
    {
        if (observer.Gold >= observer.GoldAtEntry || observer.Gold > LowGoldThreshold)
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
            Priority = 5,
            Tags = new[] { UtteranceTag.Shop },
        };
    }
}
