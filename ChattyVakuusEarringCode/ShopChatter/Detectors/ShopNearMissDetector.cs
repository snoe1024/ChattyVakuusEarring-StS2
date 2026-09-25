using System.Linq;
using ChattyVakuusEarring.ChattyVakuusEarringCode.Chatter.Speaker;
using MegaCrit.Sts2.Core.Entities.Merchant;
using MegaCrit.Sts2.Core.Localization;

namespace ChattyVakuusEarring.ChattyVakuusEarringCode.ShopChatter.Detectors;

/// <summary>
/// あと少し(5ゴールド以下)のゴールドがあれば買えていたレリックがあったときに、ショップ画面を閉じた時の一言。
/// </summary>
public class ShopNearMissDetector : ShopDetector
{
    /// <summary>これ以下の不足額なら「あと少しだった」とみなす。</summary>
    private const int NearMissGoldGap = 5;

    private const string Topic = "SHOP_CLOSED_NEAR_MISS";

    public override ShopDetectorTrigger Triggers => ShopDetectorTrigger.ShopClosed;

    public override Utterance? Detect(ShopObserver observer, ShopDetectorTrigger trigger)
    {
        int gap = observer.StockedEntries
            .Where(e => !e.EnoughGold && (e is MerchantRelicEntry or MerchantCardRemovalEntry))
            .Select(e => e.Cost - observer.Gold)
            .DefaultIfEmpty(int.MaxValue)
            .Min();

        if (gap <= 0 || gap > NearMissGoldGap)
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
