using ChattyVakuusEarring.ChattyVakuusEarringCode.Chatter.Observer;
using ChattyVakuusEarring.ChattyVakuusEarringCode.Chatter.Speaker;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models.Relics;

namespace ChattyVakuusEarring.ChattyVakuusEarringCode.Chatter.Detectors.Relics;

/// <summary>
/// 「オリハルコンが泣いてますよ」。ブロックを中途半端に持ったままターンを終えようとした時の小言。
/// </summary>
/// <remarks>
/// オリハルコンはターン終了時にブロックが0なら自動で(既定で)6ブロックを得る。
/// ブロックが1以上6未満の状態でターンを終えると、それが発動せず、6ブロック得るより少ない状態で敵の攻撃を受けることになる。
/// 言うのは、敵の攻撃が(既定で)6以上ある場合だけ。ターン終了時にプレートで6以上になる場合と、
/// 敵の攻撃の予測が狂う巨人(Colossus)所持時は言わない。
/// </remarks>
public sealed class OrichalcumComplaintDetector : PlayDetector
{
    private const string Topic = "ORICHALCUM_COMPLAINT";

    public override DetectorTrigger Triggers => DetectorTrigger.PlayerEndedTurn;

    public override bool ShouldActivate(PlayObserver observer) => observer.HasRelic<Orichalcum>();

    public override Utterance? Detect(PlayObserver observer, DetectorTrigger trigger)
    {
        Orichalcum relic = observer.GetRelic<Orichalcum>()!;
        int autoBlock = relic.DynamicVars.Block.IntValue;
        int block = observer.Block;

        if (block < 1 || block >= autoBlock)
        {
            return null;
        }

        if (observer.HasPower<ColossusPower>())
        {
            return null;
        }

        if (block + observer.GetPowerAmount<PlatingPower>() >= autoBlock)
        {
            return null;
        }

        if (observer.EstimateIncomingAttackDamage() < autoBlock)
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
