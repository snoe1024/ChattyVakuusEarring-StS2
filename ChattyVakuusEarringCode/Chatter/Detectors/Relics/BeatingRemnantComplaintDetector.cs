using ChattyVakuusEarring.ChattyVakuusEarringCode.Chatter.Observer;
using ChattyVakuusEarring.ChattyVakuusEarringCode.Chatter.Speaker;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models.Relics;

namespace ChattyVakuusEarring.ChattyVakuusEarringCode.Chatter.Detectors.Relics;

/// <summary>
/// 「脈打つ亡骸が泣いてますよ」。ブロックを積んだのに、積まなくても結果が変わらないターンの終え方をした時の小言。
/// </summary>
/// <remarks>
/// 脈打つ亡骸は1ターンに失うHPを(既定で)20までに抑える。ブロックがあっても、それを引いてなお
/// 残りの上限を超えるダメージが来るなら、失うHPは上限ぴったりで、ブロックの有無は結果に効いていない。
/// </remarks>
public sealed class BeatingRemnantComplaintDetector : PlayDetector
{
    private const string Topic = "BEATING_REMNANT_COMPLAINT";

    public override DetectorTrigger Triggers => DetectorTrigger.PlayerEndedTurn;

    // 敵の攻撃ダメージの予測はローカルプレイヤー視点でしか正確に出せない。
    public override bool ShouldActivate(PlayObserver observer) => observer.HasRelic<BeatingRemnant>();

    public override Utterance? Detect(PlayObserver observer, DetectorTrigger trigger)
    {
        BeatingRemnant relic = observer.GetRelic<BeatingRemnant>()!;
        int block = observer.Block;
        if (block <= 0)
        {
            return null;
        }

        // 上限は「プレイヤーのターン+続く敵のターン」で1回分。ここまでに今ターン失ったHPを引いた残りが、これから受けられる分。
        int remainingCap = relic.DynamicVars["MaxHpLoss"].IntValue - observer.HpLostThisTurn();
        int unblockedIncoming = observer.EstimateIncomingAttackDamage() - block;
        if (unblockedIncoming <= remainingCap)
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
            Probability = 0.7f,
            Tags = new[] { UtteranceTag.RelicComplaint, UtteranceTag.Blocking },
        };
    }
}
