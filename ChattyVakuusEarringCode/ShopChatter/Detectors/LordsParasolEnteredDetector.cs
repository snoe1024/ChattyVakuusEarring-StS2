using ChattyVakuusEarring.ChattyVakuusEarringCode.Chatter.Speaker;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models.Relics;

namespace ChattyVakuusEarring.ChattyVakuusEarringCode.ShopChatter.Detectors;

/// <summary>
/// 王のパラソル(<see cref="LordsParasol"/>、入室時に店の在庫を全部無料で買い占めるレリック)持ちが入室した時の一言。
/// 通常の<see cref="ShopEnteredDetector"/>とは話題を分ける(選ぶ余地が無い入室なので、内容が根本的に違うため)。
/// </summary>
public class LordsParasolEnteredDetector : ShopDetector
{
    private const string Topic = "SHOP_ENTERED_WITH_PARASOL";

    public override ShopDetectorTrigger Triggers => ShopDetectorTrigger.RoomEntered;

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