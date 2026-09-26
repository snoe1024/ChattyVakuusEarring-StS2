using System.Linq;
using ChattyVakuusEarring.ChattyVakuusEarringCode.Chatter.Observer;
using ChattyVakuusEarring.ChattyVakuusEarringCode.Chatter.Speaker;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Powers;

namespace ChattyVakuusEarring.ChattyVakuusEarringCode.Chatter.Detectors;

/// <summary>
/// 生成されたカードがオスティ由来のダメージを与えるカードなのに、オスティがまだ召喚されておらず、
/// 手札に召喚する手段も無い時の一言(=数字だけ見て拾ったが、実質何もしないカード)。
/// </summary>
/// <remarks>
/// 「このキャラクターはそもそもオスティを召喚できない」で除外する方法も考えたが、フレンドカード獲得
/// イベントやマルチプレイの全員召喚カード配布等により、キャラクター単体からは正確に判定できない
/// (ユーザー指摘)。そこで代わりに、実際に観測できる状態(<c>Player.IsOstyAlive</c>、手札に
/// <c>PlayObserver.SummonsOsty</c>なカードが無いか)だけで判定する。
/// </remarks>
public sealed class OstyAttackNoOstyDetector : PlayDetector
{
    private const string Topic = "OSTY_ATTACK_NO_OSTY";

    public override DetectorTrigger Triggers => DetectorTrigger.CardGenerated;

    public override bool ShouldActivate(PlayObserver observer) => observer.Owner.Character is not Necrobinder;

    public override Utterance? Detect(PlayObserver observer, DetectorTrigger trigger)
    {
        CardModel? card = observer.LastGeneratedCard;
        if (card == null || !PlayObserver.IsOstySourcedAttack(card))
        {
            return null;
        }

        // グローバルフック(レリック・パワー等)を含めない、このカード固有の実効コストが0なら、そのカードはそのターン生成されたかもしれない。
        if (card.EnergyCost.GetWithModifiers(CostModifiers.Local) == 0)
        {
            return null;
        }
        
        if (card.CanPlay(out var reason, out _) || (reason & UnplayableReason.NoLivingAllies) == 0)
        {
            return null;
        }
        
        // 手札に召喚を行うカードがない
        // 召喚がついてくるパワーがないことも調べたいがこれはうまくいかなそう → observer.Owner.Creature.Powers.Any(p => p.DynamicVars.ContainsKey("Summon"))
        // なので直接調べる
        if (observer.Hand.Any(PlayObserver.SummonsOsty) || observer.HasPower<DevourLifePower>())
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
            Probability = 1f,
            Tags = new[] { UtteranceTag.CardDrawn },
        };
    }
}
