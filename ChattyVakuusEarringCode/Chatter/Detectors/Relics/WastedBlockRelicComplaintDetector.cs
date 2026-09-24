using ChattyVakuusEarring.ChattyVakuusEarringCode.Chatter.Observer;
using ChattyVakuusEarring.ChattyVakuusEarringCode.Chatter.Speaker;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models.Relics;

namespace ChattyVakuusEarring.ChattyVakuusEarringCode.Chatter.Detectors.Relics;

/// <summary>
/// 「{レリック}が泣いてますよ」。ターン中に自動でブロックを与えるレリック
/// (音叉・扇子・銀河の塵・波紋の鉢・マントの留め具)が発動した瞬間、それより前にプレイしたブロックカードの中に、
/// 丸ごと無駄になっていたと言えるものがあれば言う。
/// </summary>
/// <remarks>
/// <para>
/// 「発動した」こと自体は<see cref="Patch.BlockRelicFlashPatch"/>がHarmonyで捉えて
/// <see cref="DetectorTrigger.BlockRelicActivated"/>として届ける(通常の<c>ChatterHub</c>のイベント購読だけでは、
/// どのレリックが発動したかまでは分からないため)。
/// </para>
/// <para>
/// 「無駄だったか」の判定: レリック発動後の総ブロック(現在のブロック+このレリックが与える分。<c>Flash()</c>は
/// 実際の付与<c>await</c>より前に呼ばれるため、レリック自身の付与量は<c>DynamicVars.Block</c>から見込みで足す)と、
/// 想定被弾量(<see cref="PlayObserver.EstimateIncomingAttackDamage"/>)の差(=余剰)を求め、
/// 今ターンのカード由来ブロック獲得の中に、この余剰以下の量だったものが1つでもあれば「そのカードが無くても
/// 余裕はあった」= 丸ごと無駄、とみなす(<see cref="PlayObserver.WasBlockCardWastedThisTurn"/>)。
/// 被弾量の予測はローカルプレイヤー視点でしか正確に出せないので、自分の端末のプレイヤーに対してだけ動く。
/// </para>
/// <para>
/// バリケード(パワー)または頑丈なクランプ(レリック)を持っていると、ブロックがターンをまたいで残るため
/// 「無駄になった」という前提が崩れる。パワーは戦闘中に付いたり消えたりしうるので、
/// (戦闘開始時に1度だけ判定する)<see cref="ShouldActivate"/>ではなく毎回<see cref="Detect"/>側で確認する。
/// </para>
/// <para>戦闘中、ターンに1回だけ言う(同じターンに複数のレリックが発動しても、最初の1回だけ判定する)。</para>
/// </remarks>
public sealed class WastedBlockRelicComplaintDetector : PlayDetector
{
    public override DetectorTrigger Triggers => DetectorTrigger.BlockRelicActivated;

    public override bool ShouldActivate(PlayObserver observer) => observer.HasRelic<TuningFork>() || observer.HasRelic<GalacticDust>();

    public override Utterance? Detect(PlayObserver observer, DetectorTrigger trigger)
    {
        RelicModel? relic = observer.LastActivatedBlockRelic;
        if (relic == null)
        {
            return null;
        }

        if (observer.HasPower<BarricadePower>() || observer.HasRelic<SturdyClamp>())
        {
            return null;
        }

        if (!TryStartEvaluationForTurn(observer))
        {
            return null;
        }

        int blockAfterRelic = observer.Block + relic.DynamicVars.Block.IntValue;
        int surplus = blockAfterRelic - observer.EstimateIncomingAttackDamage();
        if (!observer.WasBlockCardWastedThisTurn(surplus))
        {
            return null;
        }

        LocString? line = SpeechTable.Pick($"{relic.Id.Entry}_COMPLAINT");
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
