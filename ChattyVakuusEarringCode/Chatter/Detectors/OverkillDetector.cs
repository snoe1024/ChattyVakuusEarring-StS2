using ChattyVakuusEarring.ChattyVakuusEarringCode.Chatter.Observer;
using ChattyVakuusEarring.ChattyVakuusEarringCode.Chatter.Speaker;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models.Powers;

namespace ChattyVakuusEarring.ChattyVakuusEarringCode.Chatter.Detectors;

/// <summary>
/// 「そこまで力を込める必要があったのですか？」。敵のHPを大きく超える、無駄の多いダメージで倒した時の小言。
/// </summary>
/// <remarks>
/// <see cref="PlayerKillDetector"/>(ただの撃破への一言)と同じ場面で出うるが、こちらの方が具体的なので
/// <see cref="Utterance.Priority"/>を高くして先に判断させている。
/// 最初のターン(ヴァクーの代打ちと、その後の敵のターンを含む)の撃破は対象外。
/// </remarks>
public sealed class OverkillDetector : PlayDetector
{
    private const string Topic = "OVERKILL";

    /// <summary>これ以上、敵のHPを超えて余ったダメージがあれば「無駄」とみなす。</summary>
    private const int OverkillThreshold = 32;
    private const float OverkillRatio = 0.1f;

    private const int OverkillPriority = 5;

    public override DetectorTrigger Triggers => DetectorTrigger.EnemyKilled;

    public override Utterance? Detect(PlayObserver observer, DetectorTrigger trigger)
    {
        // 最初のターン(ヴァクーの代打ちと、その後の敵のターンを含む)は、ヴァクーが動いたターンとその余波なので言わない。
        if (!observer.LastKillWasByOwner || observer.IsFirstTurn)
        {
            return null;
        }

        // これが最後の敵だったら別にオーバーキルしてもよさそう
        if (!observer.LivingEnemies.Any())
        {
            return null;
        }
        
        if (observer.LastKilledEnemy is null || observer.LastKilledEnemy.HasPower<MinionPower>() ||
            observer.LastKillOverkillDamage < observer.LastKilledEnemy.MaxHp * OverkillRatio ||
            observer.LastKillOverkillDamage < OverkillThreshold)
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
            Probability = 0.6f,
            Priority = OverkillPriority,
            Tags = new[] { UtteranceTag.Attacking },
        };
    }
}
