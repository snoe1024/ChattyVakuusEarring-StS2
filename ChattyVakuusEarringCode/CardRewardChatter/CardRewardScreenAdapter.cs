using System;
using Godot;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Screens.CardSelection;

namespace ChattyVakuusEarring.ChattyVakuusEarringCode.CardRewardChatter;

/// <summary><see cref="ICardChoiceScreenAdapter"/>のカード報酬画面版。</summary>
internal sealed class CardRewardScreenAdapter : ICardChoiceScreenAdapter
{
    private readonly NCardRewardSelectionScreen _screen;

    public CardRewardScreenAdapter(NCardRewardSelectionScreen screen)
    {
        _screen = screen;
    }

    public Rect2 GetViewportRect() => _screen.GetViewportRect();

    /// <summary>
    /// <c>NCardRewardSelectionScreen.GetCardHolder</c>は該当カードが見つからないと例外を投げるので、
    /// ここで吸収してnullを返す(呼び出し側で画面端フォールバックに回せるようにするため)。
    /// </summary>
    public Control? FindCardHolder(CardModel card)
    {
        try
        {
            return _screen.GetCardHolder(card);
        }
        catch (InvalidOperationException)
        {
            return null;
        }
    }
}
