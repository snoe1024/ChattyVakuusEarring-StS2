using BaseLib.Extensions;
using ChattyVakuusEarring.ChattyVakuusEarringCode.Chatter.Observer;
using ChattyVakuusEarring.ChattyVakuusEarringCode.Chatter.Speaker;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;

namespace ChattyVakuusEarring.ChattyVakuusEarringCode.Chatter.Detectors.Friends;

public class DebilitateForFriendDetector : PlayDetector
{
    private const string Topic = "DEBILITATE_FOR_FRIEND";
    
    public override DetectorTrigger Triggers => DetectorTrigger.FriendCardPlayed;
    
    public override Utterance? Detect(PlayObserver observer, DetectorTrigger trigger)
    {
        var hasDebilitate = false;

        // 敵単体か敵全体へ衰弱を付与するプレイ可能なカードがあったら示唆する　ブロックをしたいかどうかはあんまり気にしない
        // 多分ホントに"Debilitate"だけだと思うんだけど、modが他のそういうカードを実装してもおかしくないよね…
        foreach (var card in observer.Hand)
        {
            if (card.DynamicVars.ContainsKey("DebilitatePower") && card.TargetType is TargetType.AnyEnemy or TargetType.AllEnemies && card.CanPlay())
            {
                hasDebilitate = true;
                break;
            }
        }

        if (!hasDebilitate)
        {
            return null;
        }
        
        var friendCard = observer.LastFriendCardPlay;

        // 使用したカードがアタックで無いか、そもそもそのカードが衰弱付与ならスキップ
        if (friendCard?.Card.Type is not CardType.Attack || friendCard.Card.DynamicVars.ContainsKey("DebilitatePower"))
        {
            return null;
        }
        
        // 弱体が付いているが衰弱が付いていない敵単体を攻撃していたら文句を付けられる
        if (friendCard.Card.TargetType is not TargetType.AnyEnemy ||
            !friendCard.Target!.HasPower<VulnerablePower>() || friendCard.Target!.HasPower<DebilitatePower>())
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
            Probability = 0.8f,
            Tags = new[] { UtteranceTag.RelicComplaint },
        };
    }
}