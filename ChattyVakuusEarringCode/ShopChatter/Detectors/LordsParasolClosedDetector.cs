using ChattyVakuusEarring.ChattyVakuusEarringCode.Chatter.Speaker;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models.Relics;

namespace ChattyVakuusEarring.ChattyVakuusEarringCode.ShopChatter.Detectors;

/// <summary>王のパラソル持ちがショップ画面を閉じた時の一言(<see cref="LordsParasolEnteredDetector"/>の閉店版)。</summary>
public class LordsParasolClosedDetector : ShopDetector
{
    private const string Topic = "SHOP_CLOSED_WITH_PARASOL";

    public override ShopDetectorTrigger Triggers => ShopDetectorTrigger.ShopClosed;

    public override bool ShouldActivate(ShopObserver observer) => observer.HasRelic<LordsParasol>();

    public override Utterance? Detect(ShopObserver observer, ShopDetectorTrigger trigger)
    {
        LocString? line = SpeechTable.Pick(Topic);
        if (line == null)
        {
            return null;
        }

        return new Utterance
        {
            Line = line,
            Tags = new[] { UtteranceTag.Shop },
        };
    }
}
