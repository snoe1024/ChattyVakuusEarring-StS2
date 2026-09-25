using ChattyVakuusEarring.ChattyVakuusEarringCode.Chatter.Speaker;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models.Relics;

namespace ChattyVakuusEarring.ChattyVakuusEarringCode.ShopChatter.Detectors;

/// <summary>
/// 何も買えるものが無いまま、ショップ画面を開いて閉じた時の一言(<see cref="ShopEnteredDetector"/>の
/// 「入室時に何も買えない」の閉店版)。
/// </summary>
public class ShopCantAffordDetector : ShopDetector
{
    private const string Topic = "SHOP_CLOSED_CANT_AFFORD";

    public override ShopDetectorTrigger Triggers => ShopDetectorTrigger.ShopClosed;

    public override bool ShouldActivate(ShopObserver observer) => !observer.HasRelic<LordsParasol>();

    public override Utterance? Detect(ShopObserver observer, ShopDetectorTrigger trigger)
    {
        if (observer.CanAffordAnything)
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
            Tags = new[] { UtteranceTag.Shop },
        };
    }
}
