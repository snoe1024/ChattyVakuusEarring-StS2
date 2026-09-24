using ChattyVakuusEarring.ChattyVakuusEarringCode.Chatter.Observer;
using ChattyVakuusEarring.ChattyVakuusEarringCode.Chatter.Speaker;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Potions;
using MegaCrit.Sts2.Core.Rooms;

namespace ChattyVakuusEarring.ChattyVakuusEarringCode.Chatter.Detectors.Potions;

/// <summary>
/// 「枷のポーションが泣いてますよ」。敵全体の筋力を7下げれば実際の被弾を18(場合によっては32)以上
/// 減らせたのに、使わずにターンが終わった時の一言。
/// </summary>
/// <remarks>
/// 「軽減量」は<see cref="PlayObserver.EstimateDamageReductionFromLoweringStrength"/>に委ねている
/// (脆弱・弱体・巨人等の乗算補正と、自身の現在のブロックの両方を考慮した上での実際の軽減量)。
/// 3層(<c>ActIndex == 2</c>)のボス戦以外では、ボス用に温存する判断が正当なので閾値を引き上げる。
/// </remarks>
public sealed class ShacklingPotionComplaintDetector : PlayDetector
{
    /// <summary>通常時の、この程度軽減できたら勿体ないとみなす閾値。</summary>
    private const int NormalThreshold = 18;

    /// <summary>3層の非ボス戦(ボス用に温存する価値がある場面)での閾値。</summary>
    private const int ThirdLayerNonBossThreshold = 32;

    public override DetectorTrigger Triggers => DetectorTrigger.TurnEnded;

    public override Utterance? Detect(PlayObserver observer, DetectorTrigger trigger)
    {
        if (observer.WasVakuuTurn || !observer.HasUnusedPotion<ShacklingPotion>())
        {
            return null;
        }

        ShacklingPotion potion = ModelDb.Potion<ShacklingPotion>();
        int reduction = observer.EstimateDamageReductionFromLoweringStrength(potion.DynamicVars.Strength.IntValue);

        bool savingForBoss = observer.ActIndex == 2 && observer.EncounterRoomType != RoomType.Boss;
        int threshold = savingForBoss ? ThirdLayerNonBossThreshold : NormalThreshold;
        if (reduction < threshold)
        {
            return null;
        }

        LocString? line = SpeechTable.Pick($"{potion.Id.Entry}_COMPLAINT");
        if (line == null)
        {
            return null;
        }

        return new Utterance
        {
            Line = line,
            Probability = 0.6f,
            Tags = new[] { UtteranceTag.PotionComplaint, UtteranceTag.Attacking },
        };
    }
}
