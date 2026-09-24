using BaseLib.Extensions;
using ChattyVakuusEarring.ChattyVakuusEarringCode.Chatter.Observer;
using ChattyVakuusEarring.ChattyVakuusEarringCode.Chatter.Speaker;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;

namespace ChattyVakuusEarring.ChattyVakuusEarringCode.Chatter.Detectors.Friends;

public class VulnerableForFriendDetector : PlayDetector
{
    private const string Topic = "VULNERABLE_FOR_FRIEND";
    
    public override DetectorTrigger Triggers => DetectorTrigger.FriendCardPlayed;
    
    public override Utterance? Detect(PlayObserver observer, DetectorTrigger trigger)
    {
        var hasVulnerable = false;

        // 敵単体か敵全体へ弱体を付与するプレイ可能なカードがあったら示唆する　ブロックをしたいかどうかはあんまり気にしない
        foreach (var card in observer.Hand)
        {
            if (card.DynamicVars.ContainsKey("VulnerablePower") && card.TargetType is TargetType.AnyEnemy or TargetType.AllEnemies && card.CanPlay())
            {
                hasVulnerable = true;
                break;
            }
        }

        if (!hasVulnerable)
        {
            return null;
        }
        
        var friendCard = observer.LastFriendCardPlay;

        if (friendCard is null)
        {
            return null;
        }

        // 威圧だったら必ず通す
        if (friendCard.Card is Dominate)
        {
            // skip to utterance logic
        }
        else
        {
            // 使用したカードがアタックで無いか、そもそもそのカードが弱体付与ならスキップ
            if (friendCard.Card.Type is not CardType.Attack || friendCard.Card.DynamicVars.ContainsKey("VulnerablePower"))
            {
                return null;
            }
        
            // 弱体の付いていない敵単体を攻撃していたら文句を付けられる
            if (friendCard.Card.TargetType is not TargetType.AnyEnemy || friendCard.Target!.HasPower<VulnerablePower>())
            {
                return null;
            }
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