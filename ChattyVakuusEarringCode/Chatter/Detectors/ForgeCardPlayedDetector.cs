using ChattyVakuusEarring.ChattyVakuusEarringCode.Chatter.Observer;
using ChattyVakuusEarring.ChattyVakuusEarringCode.Chatter.Speaker;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models.Characters;

namespace ChattyVakuusEarring.ChattyVakuusEarringCode.Chatter.Detectors;

/// <summary>
/// 「鍛造」(リージェントのキーワード)を使うカードをプレイした時の一言。リージェント自身が使った時と、
/// それ以外のキャラクターが使った時(フレンドカード獲得イベント等でしか起こりえない)とで話題を分ける。
/// </summary>
/// <remarks>
/// 「鍛造」自体は<c>CombatHistory</c>に専用のエントリを持たない(<c>ForgeCmd.Forge</c>を直接呼ぶだけ)ため、
/// <see cref="DetectorTrigger.CardPlayed"/>で、プレイされたカードが<c>ForgeVar</c>
/// (<c>DynamicVars</c>の<c>"Forge"</c>キー)を持つかどうかで判定する
/// (<c>PlayObserver.SummonsOsty</c>と同じ「共通DynamicVarで見る」パターン)。
/// </remarks>
public sealed class ForgeCardPlayedDetector : PlayDetector
{
    private const string Topic = "FORGE_USED";

    public override DetectorTrigger Triggers => DetectorTrigger.CardPlayed;
    
    private bool _fired;

    public override Utterance? Detect(PlayObserver observer, DetectorTrigger trigger)
    {
        if (observer.IsFirstTurn || observer.LastManualCardPlay?.Card.DynamicVars.TryGetValue("Forge", out _) != true)
        {
            return null;
        }

        // リージェント以外は最初の一回しか言わない
        bool isNative = observer.Owner.Character is Regent;
        if (!isNative && _fired)
        {
            return null;
        }
        
        LocString? line = SpeechTable.Pick(Topic, isNative ? "native" : "other");
        if (line == null)
        {
            return null;
        }
        
        _fired = true;

        return new Utterance
        {
            Line = line,
            Probability = isNative ? 0.15f : 0.5f,
            Tags = new[] { UtteranceTag.CharacterMechanic },
        };
    }
}
