using Godot;
using MegaCrit.Sts2.Core.Models;

namespace ChattyVakuusEarring.ChattyVakuusEarringCode.CardRewardChatter;

/// <summary>
/// カード報酬画面(<c>NCardRewardSelectionScreen</c>)とポーション等の「カードを1枚選ぶ」画面
/// (<c>NChooseACardSelectionScreen</c>)は、見た目・挙動がほぼ同じだが本家側では型として無関係
/// (共通の基底クラスを持たない)。<see cref="CardRewardVakuuBubble"/>が吹き出しの位置決めに必要とする
/// 最小限の操作だけをここに切り出し、<see cref="CardRewardObserver"/>・Detector側はどちらの画面から
/// 呼ばれたか気にしなくて済むようにする(2026-09追加)。
/// </summary>
internal interface ICardChoiceScreenAdapter
{
    Rect2 GetViewportRect();

    /// <summary>指定したカードを表示しているホルダー(位置計算の元)。見つからなければnull。</summary>
    Control? FindCardHolder(CardModel card);
}
