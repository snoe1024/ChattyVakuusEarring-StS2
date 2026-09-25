using ChattyVakuusEarring.ChattyVakuusEarringCode.Chatter.Speaker;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models.Relics;

namespace ChattyVakuusEarring.ChattyVakuusEarringCode.ShopChatter.Detectors;

/// <summary>
/// ショップに入った時の一言。何も買えない(所持ゴールドで買えるものが1つも無い)場合は、
/// 通常とは別の話題(<c>SHOP_ENTERED.broke</c>)にする。
/// </summary>
public class ShopEnteredDetector : ShopDetector
{
    private const string Topic = "SHOP_ENTERED";

    public override ShopDetectorTrigger Triggers => ShopDetectorTrigger.RoomEntered;

    public override bool ShouldActivate(ShopObserver observer) => !observer.HasRelic<LordsParasol>();

    public override Utterance? Detect(ShopObserver observer, ShopDetectorTrigger trigger)
    {
        string? kind = observer.CanAffordAnything ? null : "broke";
        LocString? line = SpeechTable.Pick(Topic, kind);
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