using ChattyVakuusEarring.ChattyVakuusEarringCode.Chatter.Observer;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models.Potions;

namespace ChattyVakuusEarring.ChattyVakuusEarringCode.Chatter.Detectors.Potions;

/// <summary>
/// 「未来視の雫が泣いてますよ」。山札が既に0枚で、選びようが無かった状況での使用への一言。
/// </summary>
/// <remarks>
/// 未来視の雫は<c>TargetType.Self</c>なので対象は常に持ち主自身。
/// 判定は使用後(=選択が済んだ後)の山札の残り枚数を見るしかないため、「残り1枚で、使用によって
/// 0枚になった」場合(=ちゃんと仕事をした場合)と「既に0枚だった」場合を区別できない、という制約が残っている
/// (山札から手札への移動自体はドロー処理を経由しないため、履歴上の手掛かりが無い)。
/// 前者を誤って「無駄だった」と言ってしまう可能性がある、という限度付きの近似であることに注意。
/// </remarks>
public sealed class DropletOfPrecognitionMisuseDetector : PotionMisuseDetector<DropletOfPrecognition>
{
    protected override float Probability => 0.6f;

    protected override string Topic => "DROPLET_OF_PRECOGNITION_MISUSE";

    protected override bool WasWasted(PlayObserver observer, DropletOfPrecognition potion, Creature target) =>
        PlayObserver.DrawPileCountOf(target) == 0;
}
