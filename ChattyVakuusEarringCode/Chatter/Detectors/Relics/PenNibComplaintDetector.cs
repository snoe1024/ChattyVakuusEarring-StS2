using ChattyVakuusEarring.ChattyVakuusEarringCode.Chatter.Observer;
using ChattyVakuusEarring.ChattyVakuusEarringCode.Chatter.Speaker;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models.Relics;

namespace ChattyVakuusEarring.ChattyVakuusEarringCode.Chatter.Detectors.Relics;

public class PenNibComplaintDetector : PlayDetector
{
    private const string Topic = "PEN_NIB_COMPLAINT";
    
    public override DetectorTrigger Triggers => DetectorTrigger.CardPlayed;

    public override bool ShouldActivate(PlayObserver observer) => observer.HasRelic<PenNib>();
    
    private PenNib? _penNib;
    
    public override Utterance? Detect(PlayObserver observer, DetectorTrigger trigger)
    {
        _penNib ??= observer.Owner.GetRelic<PenNib>();

        if (observer.LastManualCardPlay is null || _penNib is not { AttacksPlayed: 0 })
        {
            return null;
        }
        
        var lastPlayedCard = observer.LastManualCardPlay.Card;
        var lastPlayedTarget = observer.LastManualCardPlay.Target;

        // 最後にプレイしたカードがアタックでないか、ターゲットがすでに死んでいたらスキップ
        if (lastPlayedCard.Type is not CardType.Attack || (lastPlayedTarget?.IsDead ?? true))
        {
            return null;
        }

        // 全体攻撃カードならペン先消費は分かる
        if (lastPlayedCard.TargetType is TargetType.AllEnemies)
        {
            return null;
        }
        
        // 手元にプレイ可能なアタックカードがまだ残っている (今プレイしたカードのコストを足して探索してもいいけど、ブロックを貼りたいときもある…)
        var beforeEnergy = observer.Energy + lastPlayedCard.EnergyCost.GetResolved();
        var playableCards = observer.Hand.Where(card =>
            card.CanPlay() && card.Type is CardType.Attack && card.EnergyCost.GetResolved() <= beforeEnergy)
            .ToList();
        
        if (playableCards.Count is 0)
        {
            return null;
        }
        
        // 今プレイしたカードのアタックダメージ。手元に残っている(まだプレイしていない)カードは実測できないので
        // 引き続きDynamicVarsからの予測(damageMultiplier)に頼るしかないが、今プレイしたカードの分だけは
        // CombatHistoryに記録された実測値をそのまま使う(CalculatedDamageVar等、予測計算では再現しきれない
        // カードがあるため。倍率の再現もヒット回数の考慮も不要になる)。
        var damageMultiplier = (observer.Owner.Creature.HasPower<WeakPower>() ? 0.75m : 1.0m) *
                               (lastPlayedTarget.HasPower<VulnerablePower>() ? 1.5m : 1.0m);
        var lastAttackDamage = (decimal)observer.GetRealizedDamage(observer.LastManualCardPlay);

        var complaintable = false;
        var requiredDamage = lastPlayedTarget.CurrentHp + lastAttackDamage * 2;
        foreach (var card in playableCards)
        {
            decimal predictedDamage = PlayObserver.GetAttackDamageVar(card) switch
            {
                CalculatedDamageVar calculatedDamageVar => calculatedDamageVar.Calculate(lastPlayedTarget),
                { } damageVar => damageVar.PreviewValue,
                null => -1m,
            };
            if (predictedDamage < 0m)
            {
                break;
            }
            
            predictedDamage *= damageMultiplier * (card.DynamicVars.TryGetValue("Repeat", out var pc) ? pc.IntValue : 1);

            // エナジーを緩めに見たリーサル逃しチェック
            if (requiredDamage < predictedDamage * 2)
            {
                complaintable = true;
                break;
            }
            
            // エナジーを厳しめに見たダメージ効率チェック
            if (card.EnergyCost.Canonical <= observer.Energy)
            {
                // 0コストなら単純にダメージ値が今プレイしたカードより大きかったものはないかチェック
                if (card.EnergyCost.Canonical is 0)
                {
                    if (predictedDamage > lastAttackDamage)
                    {
                        complaintable = true;
                        break;
                    }
                }
                else
                {
                    // ダメージコストパフォーマンスがより高いかつ実ダメージも高いであろうカードを探す
                    if (predictedDamage / card.EnergyCost.Canonical > lastAttackDamage / lastPlayedCard.EnergyCost.Canonical &&
                        predictedDamage > lastAttackDamage)
                    {
                        complaintable = true;
                        break;
                    }
                }
            }
        }
        
        if (!complaintable)
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
            Tags = new[] { UtteranceTag.RelicComplaint, UtteranceTag.Attacking },
        };
    }
}