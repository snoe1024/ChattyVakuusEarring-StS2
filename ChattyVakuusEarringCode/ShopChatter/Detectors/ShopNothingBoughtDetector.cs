using ChattyVakuusEarring.ChattyVakuusEarringCode.Chatter.Speaker;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models.Relics;

namespace ChattyVakuusEarring.ChattyVakuusEarringCode.ShopChatter.Detectors;

/// <summary>
/// 何か買えるものがあったのに、何も買わずにショップ画面を閉じた時の一言。
/// </summary>
public class ShopNothingBoughtDetector : ShopDetector
{
    private const string Topic = "SHOP_CLOSED_NOTHING_BOUGHT";

    public override ShopDetectorTrigger Triggers => ShopDetectorTrigger.ShopClosed;

    public override bool ShouldActivate(ShopObserver observer) => !observer.HasRelic<LordsParasol>();

    public override Utterance? Detect(ShopObserver observer, ShopDetectorTrigger trigger)
    {
        if (observer.Gold != observer.GoldAtEntry || !observer.CanAffordAnything)
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
