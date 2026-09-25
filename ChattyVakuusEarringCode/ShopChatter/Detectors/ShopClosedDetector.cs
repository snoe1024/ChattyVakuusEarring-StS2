using ChattyVakuusEarring.ChattyVakuusEarringCode.Chatter.Speaker;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models.Relics;

namespace ChattyVakuusEarring.ChattyVakuusEarringCode.ShopChatter.Detectors;

/// <summary>
/// ショップ画面を閉じた時の、汎用の一言(何かを買った時)。他の細かい状況(王のパラソル持ち、何も買わなかった、
/// 何も買えなかった、あと少しで買えた、ほぼ使い切った)は、それぞれ専用のDetectorに分けている。
/// </summary>
public class ShopClosedDetector : ShopDetector
{
    private const string Topic = "SHOP_CLOSED";

    public override ShopDetectorTrigger Triggers => ShopDetectorTrigger.ShopClosed;

    public override bool ShouldActivate(ShopObserver observer) => !observer.HasRelic<LordsParasol>();

    public override Utterance? Detect(ShopObserver observer, ShopDetectorTrigger trigger)
    {
        if (observer.Gold >= observer.GoldAtEntry)
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
            Tags = new[] { UtteranceTag.Shop },
        };
    }
}
