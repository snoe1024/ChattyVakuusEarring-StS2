using ChattyVakuusEarring.ChattyVakuusEarringCode.Chatter.Observer;
using ChattyVakuusEarring.ChattyVakuusEarringCode.Chatter.Speaker;
using MegaCrit.Sts2.Core.Localization;

namespace ChattyVakuusEarring.ChattyVakuusEarringCode.Chatter.Detectors;

/// <summary>
/// 「あの敵の構え…受け止めきれますか？」。敵の次の攻撃がそのまま通ると命に関わる時の警告。
/// </summary>
/// <remarks>
/// 敵の行動予定(意図)を見て判定する、「ヴァクーが敵を見ている」ことを最も分かりやすく示すDetector。
/// ターン開始時に、<b>今ターン何もしなければ</b>致死級の攻撃が来ることを警告(<c>turn_start</c>)し、
/// ターン終了を押した時に、<b>今のブロックでは</b>致死級の攻撃が通ってしまうことを咎める(<c>turn_end</c>)。
/// 攻撃ダメージの予測はローカルプレイヤー視点でしか正確に出せないので、自分の端末のプレイヤーに対してだけ動く。
/// 最初のターン(ヴァクーの代打ちと、その後の敵のターンを含む)では動かない。
/// </remarks>
public sealed class LethalThreatDetector : PlayDetector
{
    private const string Topic = "LETHAL_THREAT";

    public override DetectorTrigger Triggers =>
        DetectorTrigger.PlayerTurnStarted | DetectorTrigger.PlayerEndedTurn;

    public override Utterance? Detect(PlayObserver observer, DetectorTrigger trigger)
    {
        // 最初のターンはヴァクーが代打ちしていて、プレイヤーの意思で動いたターンではないので、咎めも警告もしない。
        if (observer.IsFirstTurn)
        {
            return null;
        }

        int damageAfterBlock = observer.EstimateIncomingAttackDamage() - observer.Block;
        if (damageAfterBlock < observer.Hp)
        {
            return null;
        }

        bool endingTurn = trigger == DetectorTrigger.PlayerEndedTurn;
        LocString? line = SpeechTable.Pick(Topic, endingTurn ? "turn_end" : "turn_start");
        if (line == null)
        {
            return null;
        }

        return new Utterance
        {
            Line = line,
            Probability = endingTurn ? 0.9f : 0.6f,
            Tags = new[] { UtteranceTag.Danger },
        };
    }
}
