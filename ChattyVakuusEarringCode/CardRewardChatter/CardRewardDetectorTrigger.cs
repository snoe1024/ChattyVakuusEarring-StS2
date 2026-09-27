using System;

namespace ChattyVakuusEarring.ChattyVakuusEarringCode.CardRewardChatter;

/// <summary>
/// カード報酬画面でDetectorが判定を行うタイミング。戦闘中の<c>Chatter.Observer.DetectorTrigger</c>や
/// ショップの<c>ShopChatter.ShopDetectorTrigger</c>とは完全に別物(<c>ICombatState</c>にも部屋にも依存しない)。
/// </summary>
/// <remarks>
/// 選択・スキップ等、別のタイミングが必要になったら値を足す。「どの画面から来たか」自体は別の軸
/// (呼び出し元=いつ判定するかという意味でのタイミングの違い)なのでここに含めているが、「報酬の中身が
/// レア確定かどうか」のような画面の中身についての条件は、Triggerを増やすのではなく
/// <see cref="CardRewardObserver"/>のプロパティ(<c>IsGuaranteedRareReward</c>等)を各Detectorの
/// <c>Detect</c>内でif判定する(2026-09、ユーザー確認: 中身の違いは<c>Observer</c>を通じて`Detect`内で
/// 分岐する方針に統一。Triggerを条件の数だけ増やすと組み合わせ爆発するため)。
/// </remarks>
[Flags]
public enum CardRewardDetectorTrigger
{
    None = 0,

    /// <summary>カード報酬画面(<c>NCardRewardSelectionScreen</c>)が表示された直後(選択肢が出揃った状態)。</summary>
    Shown = 1 << 0,

    /// <summary>
    /// ポーション(アタックポーション等)やカード生成系のカードが使われた時の「カードを1枚選ぶ」画面
    /// (<c>NChooseACardSelectionScreen</c>、<c>CardSelectCmd.FromChooseACardScreen</c>経由)が表示された直後。
    /// 見た目・UIの構造はカード報酬画面とほぼ同じだが、型として無関係な別画面のため独立したTriggerにしている
    /// (2026-09追加、ユーザー要望)。
    /// </summary>
    ChooseACardShown = 1 << 1,
}
