using MegaCrit.Sts2.Core.Models;

namespace ChattyVakuusEarring.ChattyVakuusEarringCode.CardRewardChatter;

/// <summary>
/// このプレイヤーが過去(現在表示中の画面より前)にカード報酬等で選んだ・見送ったカード1件分の記録。
/// <see cref="CardRewardObserver.PastCardChoices"/>参照。
/// </summary>
/// <param name="CardId">カードの種類(アップグレード状態を問わない。例: "Strike")。</param>
/// <param name="UpgradeLevel">その時点でのアップグレード段階(0=未アップグレード)。</param>
/// <param name="WasPicked">選んだ(デッキに加えた)ならtrue、見送った(報酬でスキップされた)ならfalse。</param>
public readonly record struct PastCardChoice(ModelId? CardId, int UpgradeLevel, bool WasPicked)
{
    public bool WasUpgraded => UpgradeLevel > 0;
}
