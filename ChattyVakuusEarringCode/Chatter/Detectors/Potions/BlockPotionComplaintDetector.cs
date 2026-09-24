using ChattyVakuusEarring.ChattyVakuusEarringCode.Chatter.Observer;
using ChattyVakuusEarring.ChattyVakuusEarringCode.Chatter.Speaker;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Potions;

namespace ChattyVakuusEarring.ChattyVakuusEarringCode.Chatter.Detectors.Potions;

/// <summary>
/// 「ブロックポーションが泣いてますよ」。12ダメージ以上受けそうなのに、使わずにターンが終わった時の一言。
/// </summary>
/// <remarks>
/// <see cref="DetectorTrigger.TurnEnded"/>(ターン終了ボタンではなく、ターン終了時効果が全て済んだ後)で判定する。
/// オリハルコン・プレートパワー等の「ターン終了時に得るブロック」は、この時点では既に<c>Block</c>に
/// 反映済みなので、別途加算する必要はない。
/// </remarks>
public sealed class BlockPotionComplaintDetector : PlayDetector
{
    public override DetectorTrigger Triggers => DetectorTrigger.TurnEnded;

    public override Utterance? Detect(PlayObserver observer, DetectorTrigger trigger)
    {
        if (observer.WasVakuuTurn || !observer.HasUnusedPotion<BlockPotion>())
        {
            return null;
        }

        BlockPotion potion = ModelDb.Potion<BlockPotion>();
        int unblocked = observer.EstimateIncomingAttackDamage() - observer.Block;
        if (unblocked < potion.DynamicVars.Block.IntValue)
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
            Tags = new[] { UtteranceTag.PotionComplaint, UtteranceTag.Blocking },
        };
    }
}
