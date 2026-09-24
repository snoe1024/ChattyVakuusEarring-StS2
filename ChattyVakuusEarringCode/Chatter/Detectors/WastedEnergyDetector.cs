using ChattyVakuusEarring.ChattyVakuusEarringCode.Chatter.Observer;
using ChattyVakuusEarring.ChattyVakuusEarringCode.Chatter.Speaker;
using Godot;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models.Relics;

namespace ChattyVakuusEarring.ChattyVakuusEarringCode.Chatter.Detectors;

/// <summary>
/// 「エナジーが余っていますよ。使い切らないのですか？」。まだ打てるカードがあるのにターンを終えようとした時の小言。
/// </summary>
public sealed class WastedEnergyDetector : PlayDetector
{
    private const string Topic = "WASTED_ENERGY";

    public override DetectorTrigger Triggers => DetectorTrigger.PlayerEndedTurn;
    
    // アイスクリームやパエルの涙を持っている場合はエナジー残しが正当化されるので無効化する
    public override bool ShouldActivate(PlayObserver observer) =>
        !observer.HasRelic<IceCream>() && !observer.HasRelic<PaelsTears>();

    public override Utterance? Detect(PlayObserver observer, DetectorTrigger trigger)
    {
        // エナジーが残っていて、手元にプレイできる0コストを超えたカードがある。
        if (observer.Energy < 1 || !observer.Hand.Any(card => card.CanPlay() && card.EnergyCost.GetResolved() > 0))
        {
            return null;
        }
        
        // アタックできるカードが残っている
        var remainAttackCard = observer.Hand.Any(card => card.CanPlay() && card.Type is CardType.Attack);
        
        // ブロックできるカードが残っているが、敵からのダメージ値が現在のブロック値を超えている。
        var damageAfterBlock = observer.EstimateIncomingAttackDamage() - observer.Block;
        var canBlock = observer.Hand.Where(card => card.CanPlay())
            .Sum(card => PlayObserver.GetBlockVar(card) is { } b ? Math.Floor(b.PreviewValue) : 0);
        
        // アタックできるカードが残っていないかつブロック出来るダメージがないかブロックできるカードが無いならスキップ
        if (!remainAttackCard && (damageAfterBlock <= 0 || canBlock <= 0))
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
            Probability = 0.4f,
            Tags = new[] { UtteranceTag.Energy },
        };
    }
}
