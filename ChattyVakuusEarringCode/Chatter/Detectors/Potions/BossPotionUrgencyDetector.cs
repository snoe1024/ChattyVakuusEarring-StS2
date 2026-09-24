using ChattyVakuusEarring.ChattyVakuusEarringCode.Chatter.Observer;
using ChattyVakuusEarring.ChattyVakuusEarringCode.Chatter.Speaker;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Rooms;

namespace ChattyVakuusEarring.ChattyVakuusEarringCode.Chatter.Detectors.Potions;

/// <summary>
/// 「今でないなら、{ポーション}をいつ使うというのです？」系の共通実装。ターンを経るごとに効果が積み上がる
/// (=早く使うほど得な)ボス戦向けポーションを、プレイヤーの実質的な最初のターンが終わっても使っていなければ言う。
/// </summary>
/// <remarks>
/// <para>
/// パワーポーション(ドローしたパワーを即戦力にできる)・終焉の粉末(<c>DemisePower</c>は経過ターンに応じて
/// ダメージが増す)・マザレスの贈り物(<c>RitualPower</c>は毎ターン筋力が増える)が対象。3つとも構造は同じ
/// (「このポーションを持っている」×「実質1ターン目が終わった」×「まだ使っていない」)なので、この基底クラスに
/// 集約し、サブクラスは型引数を与えるだけで済む。
/// </para>
/// <para>
/// ダブルボス(Ascension「DoubleBoss」)の1戦目は、直後にもう一度ボス戦が控えているので、そちらのために
/// 温存する判断が正当になる。この場合はDetector自体を無効化する
/// (<see cref="PlayObserver.IsFirstOfDoubleBossEncounter"/>)。
/// </para>
/// <para>
/// 「プレイヤーの実質的な最初のターン」は、囁きのイヤリングを持っていれば2ターン目(1ターン目はヴァクーの
/// 代打ちなので、プレイヤー自身の判断とは言えない)、持っていなければ1ターン目。
/// </para>
/// </remarks>
public abstract class BossPotionUrgencyDetector<TPotion> : PlayDetector where TPotion : PotionModel
{
    private const string Topic = "BOSS_POTION_URGENCY";

    public override DetectorTrigger Triggers => DetectorTrigger.TurnEnded;

    public override bool ShouldActivate(PlayObserver observer) =>
        observer.EncounterRoomType == RoomType.Boss && !observer.IsFirstOfDoubleBossEncounter;

    public override Utterance? Detect(PlayObserver observer, DetectorTrigger trigger)
    {
        int playersFirstRealTurn = observer.HasRelic<WhisperingEarring>() ? 2 : 1;
        if (observer.TurnNumber != playersFirstRealTurn || !observer.HasUnusedPotion<TPotion>())
        {
            return null;
        }

        LocString? line = SpeechTable.Pick(Topic);
        if (line == null)
        {
            return null;
        }

        line.Add("Potion", ModelDb.Potion<TPotion>().Title);
        return new Utterance
        {
            Line = line,
            Probability = 0.7f,
            Tags = new[] { UtteranceTag.PotionComplaint },
        };
    }
}
